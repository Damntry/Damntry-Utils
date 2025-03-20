using System;
using System.Reflection;
using Damntry.Utils.ExtensionMethods;

namespace Damntry.Utils.Reflection {

	public class MemberInfoHelper : MemberInfo {

		private readonly MemberInfo member;

		private FieldInfo Field => (FieldInfo)member;

		private PropertyInfo Property => (PropertyInfo)member;


		public MemberInfoHelper(MemberInfo memberInfo) {
			if (memberInfo == null) {
				throw new ArgumentNullException("member");
			}
			if (memberInfo is not FieldInfo && memberInfo is not PropertyInfo) {
				throw new NotSupportedException("Only FieldInfo and PropertyInfo members are supported.");
			}

			this.member = memberInfo;
		}

		public bool IsStatic =>
				member.MemberType switch {
					MemberTypes.Field => Field.IsStatic,
					MemberTypes.Property => Property.IsStatic(),
					_ => throw new NotImplementedException()
				};

		public Type MemberInfoType =>
				member.MemberType switch {
					MemberTypes.Field => Field.FieldType,
					MemberTypes.Property => Property.PropertyType,
					_ => throw new NotImplementedException()
				};

		public void SetValue(object obj, object value) {
			if (member.MemberType == MemberTypes.Field) {
				Field.SetValue(obj, value);
			} else if (member.MemberType == MemberTypes.Property) {
				Property.SetValue(obj, value);
			}
		}

		public object GetValue(object obj) {
			if (member.MemberType == MemberTypes.Field) {
				return Field.GetValue(obj);
			} else if (member.MemberType == MemberTypes.Property) {
				return Property.GetValue(obj);
			}
			throw new NotImplementedException();
		}

		public object GetValueStaticAgnostic(object obj) {
			obj = IsStatic ? null : obj;

			if (member.MemberType == MemberTypes.Field) {
				return Field.GetValue(obj);
			} else if (member.MemberType == MemberTypes.Property) {
				return Property.GetValue(obj);
			}
			throw new NotImplementedException();
		}



		public override MemberTypes MemberType => member.MemberType;

		public override string Name => member.Name;

		public override Type DeclaringType => member.DeclaringType;

		public override Type ReflectedType => member.ReflectedType;

		public override object[] GetCustomAttributes(bool inherit) =>
			member.GetCustomAttributes(inherit);

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) =>
			member.GetCustomAttributes(attributeType, inherit);

		public override bool IsDefined(Type attributeType, bool inherit) =>
			member.IsDefined(attributeType, inherit);
	}

}
