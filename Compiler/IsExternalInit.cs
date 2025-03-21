#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices { //Namespace "System.Runtime.CompilerServices" should be kept.

	//So the compiler shuts up about this Type not defined or imported, when 
	//	not targeting net5.0 and using certain features (init setter, records...)
	public class IsExternalInit { }
}
#endif
