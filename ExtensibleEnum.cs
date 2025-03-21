using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Damntry.Utils {

	/* Failed attempt, too many small problems with no good solution. 
	 * Maybe I should just use Templates to generate an enum.

	[StructLayout(LayoutKind.Sequential)]	//To make sure GetFields returns "enum" members in the correct order.
	public abstract class ExtensibleEnum {

		public abstract ulong Flags { get; set; }


		public static readonly ExtensibleEnum None;



		public ulong Value { get; internal set; }

		internal string _name;


		public override string ToString() {
			return _name;
		}


		protected static void InitializeEnums<T>()
				where T : ExtensibleEnum {

			Type derivedType = typeof(T);

			var staticExtEnums = derivedType
				.GetFields(BindingFlags.Static | BindingFlags.Public)
				//Keep declaration order. Needs [StructLayout(LayoutKind.Sequential)] to work.
				.OrderBy(f => Marshal.OffsetOf(derivedType, f.Name).ToInt32())
				.Select(fldInfo => fldInfo);

			//Currently, if derivedType inherits from a class that itself derives from
			//		ExtensibleEnum, the code below redoes the instances and setting values of members
			//		of the parent class too.
			//		I need to do it like this because I havent found a good way of holding the last counter
			//		value used at each derived ExtensiveEnum type, so instead I just redo everything from the start.
			//		Cant use a static in ExtensiveEnum since I want counter values to start from 1 at the class
			//		deriving ExtensiveEnum itself.
			//		A solution would be a static property on each derived class holding that last counter
			//		value used, but its not worth adding more boilerplate for a case that is not really so much a
			//		problem, but an annoyance that could bite me in the ass if I start using this class in unintended ways.
			ulong counter = 1;

			foreach (FieldInfo staticExtEnum in staticExtEnums) {
				ExtensibleEnum instance = Activator.CreateInstance<ExtensibleEnum>();
				instance._name = staticExtEnum.Name;
				instance.Value = counter;
				counter <<= 1;

				staticExtEnum.SetValue(null, instance);
			}
		}


		public bool HasFlag(ulong flags) {
			if (flags == 0) {
				//Otherwise the check below would always return true.
				return false;
			}
			return (Flags & flags) == flags;
		}

	}
	*/
	/* Example of implementation:

	//My problem is that both the class and the containing member is LogCategories, so if I want
	//		to do something like the class having functionality that the containing member does not have,
	//		like for example set the flags on a new instance, but not on an instance of TempTest, I cant.
	//	Maybe I can separate the class logic of each? The main class inherits from ExtensibleEnum, and the
	//		members are made of a different class that is a subclass within ExtensibleEnum, that will have
	//		its own inherited class, that has the specific functionality.
	[StructLayout(LayoutKind.Sequential)]
	public class LogCategories : ExtensibleEnum {

		//Static fields are initialized in textual order, so the first one to initialize 
		//	would be Null, then TempTest, etc... This keeps internal counter numerical order.
		public static readonly LogCategories TempTest;  //Temporary tests not meant for release.
		public static readonly LogCategories Vanilla;
		public static readonly LogCategories PerfTest;  //Temporary performance tests not meant for release.
		public static readonly LogCategories Loading;
		public static readonly LogCategories Task;      //Task, threaded or not, related logic.
		public static readonly LogCategories Reflect;   //Reflection
		public static readonly LogCategories Config;
		public static readonly LogCategories PerfCheck;    //Performance checks that calculate and/or does dynamic throttling.

		public override ulong Flags { get; set; }

		static LogCategories() {
			ExtensibleEnum.InitializeEnums<LogCategories>();
		}

	}
	*/


}
