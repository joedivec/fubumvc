using System;
using System.Linq.Expressions;
using FubuCore.Reflection;
using FubuMVC.UI.Configuration;
using FubuMVC.UI.Security;
using NUnit.Framework;

namespace FubuMVC.Tests.UI.Security
{
    [TestFixture]
    public class FieldAccessRegistryIntegratedTester
    {
        private FieldAccessRegistry registry;

        [SetUp]
        public void SetUp()
        {
            registry = new FieldAccessRegistry();
            registry.AddSecurityRule(new JNameRule());
            registry.AddLogicRule(new LimiterRule<PersonModel>(person => person.Age > 30, AccessRight.ReadOnly));
        }

        private AccessRight RightsFor<T>(T model, Expression<Func<T, object>> expression)
        {
            var request = ElementRequest.For(model, expression);
            return registry.RightsFor(request);
        }

        [Test]
        public void no_rules_of_any_kind_apply()
        {
            RightsFor<TestInputModel>(new TestInputModel(), x => x.Age).ShouldEqual(AccessRight.All);
        }

        [Test]
        public void logic_rule_applies_but_no_security()
        {
            // Logic rule should limit anything over 30
            RightsFor(new PersonModel(){Age = 31}, x => x.Age).ShouldEqual(AccessRight.ReadOnly);
            RightsFor(new PersonModel(){Age = 29}, x => x.Age).ShouldEqual(AccessRight.All);
        }

        [Test]
        public void security_rule_applies_but_no_logic()
        {
            RightsFor(new PlayerModel(){Name = "Jeremy"}, x => x.Name).ShouldEqual(AccessRight.All);
            RightsFor(new PlayerModel(){Name = "Chad"}, x => x.Name).ShouldEqual(AccessRight.ReadOnly);
        }
    }


    public class JNameRule : IFieldAccessRule
    {
        public AccessRight RightsFor(ElementRequest request)
        {
            if (request.Value<string>().StartsWith("J")) return AccessRight.All;

            return AccessRight.ReadOnly;
        }

        public bool Matches(Accessor accessor)
        {
            return accessor.Name == "Name";
        }
    }

    public class LimiterRule<T> : IFieldAccessRule
    {
        private readonly Func<T, bool> _filter;
        private readonly AccessRight _limitation;

        public LimiterRule(Func<T, bool> filter, AccessRight limitation)
        {
            _filter = filter;
            _limitation = limitation;
        }

        public AccessRight RightsFor(ElementRequest request)
        {
            return _filter(request.Model.As<T>()) ? _limitation : AccessRight.All;
        }

        public bool Matches(Accessor accessor)
        {
            return accessor.OwnerType == typeof (T);
        }
    }

    public class PersonModel
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class PlayerModel
    {
        public string Name { get; set; }
        public string Position { get; set; }
    }
}