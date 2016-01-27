﻿using System;
using System.Linq;
using System.Reflection;
using Autofac;
using NUnit.Framework;

namespace NodeTest
{
    public class BaseTestsAttribute : Attribute, ITestAction
    {
        private IContainer _container;
        private Type _fixtureType;
        private object _fixture;

        private void injectMembers()
        {
            injectMembers(GetType(),
                          this);

            injectMembers(_fixtureType,
                          _fixture);
        }

        private void injectMembers(IReflect type,
                                   object instance)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanWrite);
            foreach (var propertyInfo in properties)
            {
                propertyInfo.SetValue(instance,
                                      _container.Resolve(propertyInfo.PropertyType),
                                      null);
            }
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var fieldsInfo in fields)
            {
                fieldsInfo.SetValue(instance,
                                    _container.Resolve(fieldsInfo.FieldType));
            }
        }

        public void BeforeTest(TestDetails test)
        {
            _fixture = test.Fixture;
            _fixtureType = _fixture.GetType();

            buildContainer();
            injectMembers();
        }

        private void buildContainer()
        {
            var builder = new ContainerBuilder();
            SetUp(builder);
            _container = builder.Build();
        }

        protected virtual void SetUp(ContainerBuilder builder)
        {
        }

        public void AfterTest(TestDetails test)
        {
            //throw new NotImplementedException();
        }

        public ActionTargets Targets
        {
            get { return ActionTargets.Test; }
        }
    }
}