﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Stardust.Manager.Extensions;
using Stardust.Manager.Helpers;
using Stardust.Manager.Interfaces;
using Stardust.Manager.Models;

namespace Stardust.Manager
{
	public class JobRepository : IJobRepository
	{
		private readonly RetryPolicy _retryPolicy;
		private readonly string _connectionString;

		public JobRepository(string connectionString, RetryPolicyProvider retryPolicyProvider)
		{
			_connectionString = connectionString;
			_retryPolicy = retryPolicyProvider.GetPolicy();
		}
		
		public void Add(JobDefinition job)
		{
			using (var connection = new SqlConnection(_connectionString))
			{
				SqlCommand jobHistoryCommand = connection.CreateCommand();
				jobHistoryCommand.CommandText = "INSERT INTO [Stardust].JobHistory (JobId, Name, CreatedBy, Serialized, Type) VALUES(@Id, @Name, @By, @Serialized, @Type)";
				jobHistoryCommand.Parameters.AddWithValue("@Id", job.Id);
				jobHistoryCommand.Parameters.AddWithValue("@Name", job.Name);
				jobHistoryCommand.Parameters.AddWithValue("@By", job.UserName);
				jobHistoryCommand.Parameters.AddWithValue("@Serialized", job.Serialized);
				jobHistoryCommand.Parameters.AddWithValue("@Type", job.Type);

				SqlCommand jobDefinitionCommand = connection.CreateCommand();
				jobDefinitionCommand.CommandText = "INSERT INTO [Stardust].JobDefinitions (Id, Name, Serialized, Type, userName) VALUES(@Id, @Name, @Serialized, @Type, @UserName)";
				jobDefinitionCommand.Parameters.AddWithValue("@Id", job.Id);
				jobDefinitionCommand.Parameters.AddWithValue("@Name", job.Name);
				jobDefinitionCommand.Parameters.AddWithValue("@UserName", job.UserName);
				jobDefinitionCommand.Parameters.AddWithValue("@Serialized", job.Serialized);
				jobDefinitionCommand.Parameters.AddWithValue("@Type", job.Type);
				try
				{
					connection.OpenWithRetry(_retryPolicy);
					using (var tran = connection.BeginTransaction())
					{
						jobDefinitionCommand.Transaction = tran;
						jobHistoryCommand.Transaction = tran;
						jobDefinitionCommand.ExecuteNonQueryWithRetry(_retryPolicy);
						jobHistoryCommand.ExecuteNonQueryWithRetry(_retryPolicy);
						ReportProgress(job.Id, "Added", DateTime.UtcNow); 
						tran.Commit();
					}
				}
				catch (Exception exp)
				{
					this.Log().ErrorWithLineNumber(exp.Message, exp);
				}
				finally
				{
					connection.Close();
				}
			}
		}

		
		public List<JobDefinition> LoadAll()
		{
			const string selectCommand = @"SELECT  Id    
                                            ,Name
                                            ,Serialized
                                            ,Type
                                            ,UserName
                                            ,AssignedNode
											,Status
                                        FROM [Stardust].JobDefinitions";
			try
			{
				var listToReturn = new List<JobDefinition>();
				using (var connection = new SqlConnection(_connectionString))
				{
					connection.OpenWithRetry(_retryPolicy);
					var command = new SqlCommand
					{
						Connection = connection,
						CommandText = selectCommand,
						CommandType = CommandType.Text
					};
					var reader = command.ExecuteReaderWithRetry(_retryPolicy);
					if (reader.HasRows)
					{
						while (reader.Read())
						{
							var jobDefinition = new JobDefinition
							{
								Id = (Guid)reader.GetValue(reader.GetOrdinal("Id")),
								Name = (string)reader.GetValue(reader.GetOrdinal("Name")),
								Serialized = (string)reader.GetValue(reader.GetOrdinal("Serialized")),
								Type = (string)reader.GetValue(reader.GetOrdinal("Type")),
								UserName = (string)reader.GetValue(reader.GetOrdinal("UserName")),
								AssignedNode = GetValue<string>(reader.GetValue(reader.GetOrdinal("AssignedNode"))),
								Status = GetValue<string>(reader.GetValue(reader.GetOrdinal("Status")))
							};
							listToReturn.Add(jobDefinition);
						}
					}
					reader.Close();
					connection.Close();
				}
				return listToReturn;
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
			}
			return null;
		}
		
		public void DeleteJob(Guid jobId)
		{
			using (var connection = new SqlConnection(_connectionString))
			{
				SqlCommand deleteCommand = connection.CreateCommand();
				deleteCommand.CommandText = "DELETE FROM[Stardust].JobDefinitions WHERE Id = @ID";
				deleteCommand.Parameters.AddWithValue("@ID", jobId);

				try
				{
					connection.OpenWithRetry(_retryPolicy);

					using (var tran = connection.BeginTransaction())
					{
						deleteCommand.Transaction = tran;
						deleteCommand.ExecuteNonQueryWithRetry(_retryPolicy);
						tran.Commit();
					}
				}
				catch (Exception exp)
				{
					this.Log().ErrorWithLineNumber(exp.Message, exp);
				}
				finally
				{
					connection.Close();
				}
			}
		}
		
		public void FreeJobIfNodeIsAssigned(string url)
		{
			using (var connection = new SqlConnection(_connectionString))
			{
				SqlCommand deleteCommand = connection.CreateCommand();
				deleteCommand.CommandText = "Update [Stardust].JobDefinitions Set AssignedNode = null where AssignedNode = @assingedNode";
				deleteCommand.Parameters.AddWithValue("@assingedNode", url);

				try
				{
					connection.OpenWithRetry(_retryPolicy);
					using (var tran = connection.BeginTransaction())
					{
						deleteCommand.Transaction = tran;
						deleteCommand.ExecuteNonQueryWithRetry(_retryPolicy);
						tran.Commit();
					}
				}
				catch (Exception exp)
				{
					this.Log().ErrorWithLineNumber(exp.Message, exp);
				}
				finally
				{
					connection.Close();
				}
			}
		}
		
		public async void CheckAndAssignNextJob(List<WorkerNode> availableNodes,
												IHttpSender httpSender)
		{
			if (!availableNodes.Any()) return;

			try
			{
				using (var connection = new SqlConnection(_connectionString))
				{
					connection.OpenWithRetry(_retryPolicy);
					using (var tran = connection.BeginTransaction(IsolationLevel.Serializable))
					{
						using (
							var da =
								new SqlDataAdapter(
									"SELECT TOP 1 * FROM [Stardust].JobDefinitions WITH (TABLOCKX) WHERE AssignedNode IS NULL OR AssignedNode = ''",
									connection)
								{
									SelectCommand =
									{
										Transaction = tran,
										CommandTimeout = 10
									}
								})
						{
							var jobs = new DataTable();
							da.Fill(jobs);

							if (jobs.Rows.Count > 0)
							{
								var jobRow = jobs.Rows[0];
								var job = new JobToDo
								{
									Id = (Guid) jobRow["Id"],
									Name = GetValue<string>(jobRow["Name"]),
									Serialized = GetValue<string>(jobRow["Serialized"]).Replace(@"\", @""),
									Type = GetValue<string>(jobRow["Type"]),
									CreatedBy = GetValue<string>(jobRow["UserName"])
								};

								da.UpdateCommand =
									new SqlCommand(
										"UPDATE [Stardust].JobDefinitions SET AssignedNode = @AssignedNode, Status = 'Started' WHERE Id = @Id",
										connection);
								da.UpdateCommand.Parameters.AddWithValue("@Id", job.Id);
								da.UpdateCommand.Transaction = tran;

								foreach (var node in availableNodes)
								{
									try
									{
										var builderHelper = new NodeUriBuilderHelper(node.Url);
										var urijob = builderHelper.GetJobTemplateUri();
										var response = await httpSender.PostAsync(urijob, job);
										
											if(response.IsSuccessStatusCode)
											{ 
												//save
												da.UpdateCommand.Parameters.AddWithValue("@AssignedNode", node.Url.ToString());
												da.UpdateCommand.ExecuteNonQueryWithRetry(_retryPolicy);

												//update history
												da.UpdateCommand =
													new SqlCommand("UPDATE [Stardust].JobHistory SET Started = @Started, SentTo = @Node WHERE JobId = @Id",
													               connection);
												da.UpdateCommand.Parameters.AddWithValue("@Id", job.Id);
												da.UpdateCommand.Parameters.AddWithValue("@Started", DateTime.UtcNow);
												da.UpdateCommand.Parameters.AddWithValue("@Node", node.Url.ToString());
												da.UpdateCommand.Transaction = tran;
												da.UpdateCommand.ExecuteNonQueryWithRetry(_retryPolicy);

												ReportProgress(job.Id, "Started", DateTime.UtcNow);

												break;
											}
											if (response.StatusCode.Equals(HttpStatusCode.BadRequest))
											{
												//remove the job if badrequest
												da.DeleteCommand = new SqlCommand("DELETE FROM [Stardust].JobDefinitions WHERE Id = @Id",
												                                  connection);
												da.DeleteCommand.Parameters.AddWithValue("@Id", job.Id);
												da.DeleteCommand.Transaction = tran;
												da.DeleteCommand.ExecuteNonQueryWithRetry(_retryPolicy);

												//update history
												da.UpdateCommand =
													new SqlCommand("UPDATE [Stardust].JobHistory " +
													               "SET Result = @Result, SentTo = @Node WHERE JobId = @Id",
													               connection);
												da.UpdateCommand.Parameters.AddWithValue("@Id", job.Id);
												da.UpdateCommand.Parameters.AddWithValue("@Result", "Removed because of bad request");
												da.UpdateCommand.Parameters.AddWithValue("@Node", node.Url.ToString());
												da.UpdateCommand.Transaction = tran;
												da.UpdateCommand.ExecuteNonQueryWithRetry(_retryPolicy);

												//insert into history detail.
												if (response.ReasonPhrase != null)
												{
													string insertcommand = @"INSERT INTO [Stardust].[JobHistoryDetail]
																	([JobId]
																	,[Created]
																	,[Detail])
																VALUES
																	(@JobId
																	,@Created
																	,@Detail)";

													da.InsertCommand = new SqlCommand(insertcommand, connection);
													da.InsertCommand.Parameters.AddWithValue("@JobId", job.Id);
													da.InsertCommand.Parameters.AddWithValue("@Detail", response.ReasonPhrase);
													da.InsertCommand.Parameters.AddWithValue("@Created", DateTime.UtcNow);
													da.InsertCommand.Transaction = tran;
													da.InsertCommand.ExecuteNonQueryWithRetry(_retryPolicy);
												}
											}
										}
									catch (SqlException exp)
									{
										if (exp.Number == -2) //Timeout
										{
											this.Log().InfoWithLineNumber(exp.Message);
										}
										else
										{
											this.Log().ErrorWithLineNumber(exp.Message, exp);
										}
									}
									catch (Exception exp)
									{
										this.Log().ErrorWithLineNumber(exp.Message, exp);
									}
								}
							}
						}
						tran.Commit();
						connection.Close();
					}
				}
			}
			catch (SqlException exp)
			{
				if (exp.Number == -2) //Timeout
				{
					this.Log().InfoWithLineNumber(exp.Message);
				}
				else
				{
					this.Log().ErrorWithLineNumber(exp.Message, exp);
				}
			}

			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
			}
		}


		public async void CancelThisJob(Guid jobId, IHttpSender httpSender)
		{
			try
			{
				using (var connection = new SqlConnection(_connectionString))
				{
					connection.OpenWithRetry(_retryPolicy);
					var tran = connection.BeginTransaction(IsolationLevel.Serializable);

					using (var da =
						new SqlDataAdapter(string.Format("SELECT * From [Stardust].JobDefinitions WITH (TABLOCKX) WHERE Id = '{0}'",
														 jobId),
										   connection)
						{
							SelectCommand =
							{
								Transaction = tran
							}
						})
					{
						var jobs = new DataTable();
						da.Fill(jobs);
						if (jobs.Rows.Count > 0)
						{
							var jobRow = jobs.Rows[0];
							var node = GetValue<string>(jobRow["AssignedNode"]);

							if (string.IsNullOrEmpty(node))
							{
								da.DeleteCommand = new SqlCommand("DELETE FROM [Stardust].JobDefinitions WHERE Id = @Id",
																  connection);
								da.DeleteCommand.Parameters.AddWithValue("@Id", jobId);
								da.DeleteCommand.Transaction = tran;
								da.DeleteCommand.ExecuteNonQueryWithRetry(_retryPolicy);

								//update history
								da.UpdateCommand = new SqlCommand("UPDATE [Stardust].JobHistory SET Result = @Result WHERE JobId = @Id",
																  connection);
								da.UpdateCommand.Parameters.AddWithValue("@Id", jobId);
								da.UpdateCommand.Parameters.AddWithValue("@Result", "Deleted");
								da.UpdateCommand.Transaction = tran;
								da.UpdateCommand.ExecuteNonQueryWithRetry(_retryPolicy);
							}
							else
							{
								var builderHelper = new NodeUriBuilderHelper(node);
								var uriCancel = builderHelper.GetCancelJobUri(jobId);

								var response = await httpSender.DeleteAsync(uriCancel);

								if (response != null && response.IsSuccessStatusCode)
								{
									da.UpdateCommand = new SqlCommand("UPDATE [Stardust].JobDefinitions SET Status = 'Canceling' WHERE Id = @Id",
																	  connection);
									da.UpdateCommand.Parameters.AddWithValue("@Id", jobId);
									da.UpdateCommand.Transaction = tran;
									da.UpdateCommand.ExecuteNonQueryWithRetry(_retryPolicy);

									ReportProgress(jobId, "Canceling", DateTime.UtcNow);
								}
							}
						}
						else
						{
							this.Log().WarningWithLineNumber("[MANAGER, " + Environment.MachineName +
															   "] : Could not find job defintion for id : " + jobId);
						}
					}
					tran.Commit();
					connection.Close();
				}
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
			}

		}

		public void SetEndResultOnJob(Guid jobId, string result)
		{
			using (var connection = new SqlConnection(_connectionString))
			{
				SqlCommand command = connection.CreateCommand();
				command.CommandText = "UPDATE [Stardust].JobHistory SET Result = @Result, Ended = @Ended WHERE JobId = @Id";
				command.Parameters.AddWithValue("@Id", jobId);
				command.Parameters.AddWithValue("@Result", result);
				command.Parameters.AddWithValue("@Ended", DateTime.UtcNow);

				try
				{
					connection.OpenWithRetry(_retryPolicy);
					using (var tran = connection.BeginTransaction())
					{
						command.Transaction = tran;
						command.ExecuteNonQueryWithRetry(_retryPolicy);
						ReportProgress(jobId, result, DateTime.UtcNow);
						tran.Commit();
					}
				}
				catch (Exception exp)
				{
					this.Log().ErrorWithLineNumber(exp.Message, exp);
				}
				finally
				{
					connection.Close();
				}
			}
		}
		
		public void ReportProgress(Guid jobId, string detail, DateTime created)
		{
			using (var connection = new SqlConnection(_connectionString))
			{
				SqlCommand command = connection.CreateCommand();
				command.CommandText = "INSERT INTO [Stardust].JobHistoryDetail (JobId, Detail, Created) VALUES (@Id, @Detail, @Created)";
				command.Parameters.AddWithValue("@Id", jobId);
				command.Parameters.AddWithValue("@Detail", detail);
				command.Parameters.AddWithValue("@Created", created);

				try
				{
					connection.OpenWithRetry(_retryPolicy);
					using (var tran = connection.BeginTransaction())
					{
						command.Transaction = tran;
						command.ExecuteNonQueryWithRetry(_retryPolicy);
						tran.Commit();
					}
				}
				catch (Exception exp)
				{
					this.Log().ErrorWithLineNumber(exp.Message, exp);
				}
				finally
				{
					connection.Close();
				}
			}
		}



		public JobHistory History(Guid jobId)

		{
			try
			{
				var selectCommand = SelectHistoryCommand(true);
				using (var connection = new SqlConnection(_connectionString))
				{
					var command = new SqlCommand
					{
						Connection = connection,
						CommandText = selectCommand,
						CommandType = CommandType.Text
					};

					command.Parameters.AddWithValue("@JobId", jobId);

					connection.OpenWithRetry(_retryPolicy);
					using (var reader = command.ExecuteReaderWithRetry(_retryPolicy))
					{
						if (reader.HasRows)
						{
							reader.Read();
							var jobHist = NewJobHistoryModel(reader);
							return jobHist;
						}
						reader.Close();
						connection.Close();
					}
					return null;
				}
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
			}
			return null;
		}

		public IList<JobHistory> HistoryList()
		{
			try
			{
				var selectCommand = SelectHistoryCommand(false);
				var returnList = new List<JobHistory>();
				using (var connection = new SqlConnection(_connectionString))
				{
					var command = new SqlCommand
					{
						Connection = connection,
						CommandText = selectCommand,
						CommandType = CommandType.Text
					};

					connection.OpenWithRetry(_retryPolicy);
					using (var reader = command.ExecuteReaderWithRetry(_retryPolicy))
					{
						if (reader.HasRows)
						{
							while (reader.Read())
							{
								var jobHist = NewJobHistoryModel(reader);
								returnList.Add(jobHist);
							}
						}
						reader.Close();
						connection.Close();
					}
					return returnList;
				}
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
			}
			return null;
		}

		public IList<JobHistoryDetail> JobHistoryDetails(Guid jobId)
		{
			try
			{
				var selectCommand = @"SELECT  Created, Detail  FROM [Stardust].JobHistoryDetail WHERE JobId = @JobId";
				var returnList = new List<JobHistoryDetail>();
				using (var connection = new SqlConnection(_connectionString))
				{
					var command = new SqlCommand
					{
						Connection = connection,
						CommandText = selectCommand,
						CommandType = CommandType.Text
					};
					command.Parameters.AddWithValue("@JobId", jobId);

					connection.OpenWithRetry(_retryPolicy);

					using (var reader = command.ExecuteReaderWithRetry(_retryPolicy))
					{
						if (reader.HasRows)
						{
							while (reader.Read())
							{
								var detail = new JobHistoryDetail
								{
									Created = (DateTime)reader.GetValue(reader.GetOrdinal("Created")),
									Detail = (string)reader.GetValue(reader.GetOrdinal("Detail"))
								};
								returnList.Add(detail);
							}
						}
						reader.Close();
						connection.Close();
					}
					return returnList;
				}
			}

			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
			}
			return null;
		}

		private string GetValue<T>(object value)
		{
			return value == DBNull.Value
				? null
				: (string)value;
		}

		private JobHistory NewJobHistoryModel(SqlDataReader reader)
		{
			try
			{
				var jobHist = new JobHistory
				{
					Id = (Guid)reader.GetValue(reader.GetOrdinal("JobId")),
					Name = (string)reader.GetValue(reader.GetOrdinal("Name")),
					CreatedBy = (string)reader.GetValue(reader.GetOrdinal("CreatedBy")),
					SentTo = GetValue<string>(reader.GetValue(reader.GetOrdinal("SentTo"))),
					Result = GetValue<string>(reader.GetValue(reader.GetOrdinal("Result"))),
					Created = (DateTime)reader.GetValue(reader.GetOrdinal("Created")),
					Started = GetDateTime(reader.GetValue(reader.GetOrdinal("Started"))),
					Ended = GetDateTime(reader.GetValue(reader.GetOrdinal("Ended")))
				};

				return jobHist;
			}
			catch (Exception exp)
			{
				this.Log().ErrorWithLineNumber(exp.Message, exp);
			}
			return null;
		}

		private static string SelectHistoryCommand(bool addParameter)
		{
			var selectCommand = @"SELECT 
                                             JobId    
                                            ,Name
                                            ,CreatedBy
                                            ,Created
                                            ,Started
                                            ,Ended
                                            ,SentTo,
															Result
                                        FROM [Stardust].JobHistory";

			if (addParameter) selectCommand += " WHERE JobId = @JobId";

			return selectCommand;
		}

		private DateTime? GetDateTime(object databaseValue)
		{
			if (databaseValue.Equals(DBNull.Value))
				return null;

			return (DateTime)databaseValue;
		}
	}
}