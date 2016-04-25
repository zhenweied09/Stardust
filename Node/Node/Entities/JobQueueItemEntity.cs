﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Stardust.Node.Interfaces;

namespace Stardust.Node.Entities
{
	public class JobQueueItemEntity : IJobQueueItem, IValidatableObject
	{
		public Guid JobId { get; set; }

		public string Name { get; set; }

		public string Serialized { get; set; }

		public string Type { get; set; }


		public string CreatedBy { get; set; }

		public DateTime? Created { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			List<ValidationResult> list = new List<ValidationResult>();

			var pIncome = new[]
			{
				"Id"
			};

			if (this.JobId == Guid.Empty)
			{
				list.Add(new ValidationResult("Invalid job id value.", pIncome));
			}

			pIncome = new[]
			{
				"Name"
			};

			if (string.IsNullOrEmpty(Name))
			{
				list.Add(new ValidationResult("Invalid job name value.", pIncome));
			}

			pIncome = new[]
			{
				"Type"
			};

			if (string.IsNullOrEmpty(Type))
			{
				list.Add(new ValidationResult("Invalid job type value.", pIncome));
			}

			pIncome = new[]
			{
				"Serialized"
			};

			if (string.IsNullOrEmpty(Serialized))
			{
				list.Add(new ValidationResult("Invalid job Serialized value.",pIncome));
			}

			return list;
		}
	}
}