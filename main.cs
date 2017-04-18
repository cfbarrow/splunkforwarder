using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Principal;
using System.IO;


namespace ftsecuritysf
{
	public class main
	{

		static List<string> DLL_NAMES = new List<string>() { "DotNetZip.dll", "System.Management.dll","System.Management.Instrumentation.dll","OsInfo.dll"};

		public static void Main()
		{
			if (IsAdmin ()) {  // check admin before starting
				load_dll ("ftsecuritysf.Resources", DLL_NAMES);
				Deploy.Sfdeploy ();
			}
			else {
				string mess = "Please run the script as Administrator";
				Console.WriteLine(mess);
			}
		}

		public static void load_dll(string NameSpace, List<string> dlls){
			foreach (string dll in dlls) {
				WriteResourceToFile(NameSpace + "." + dll,dll);	
			}
		}
				
		// Extract file from exe to directory
		public static void WriteResourceToFile(string resourceName, string fileName)
		{
			using(var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
			{
				using(var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
				{
					resource.CopyTo(file);
				} 
			}
		}


		private static bool IsAdmin(){

			bool isElevated;
			WindowsIdentity identity = WindowsIdentity.GetCurrent();
			WindowsPrincipal principal = new WindowsPrincipal(identity);
			isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);

			return isElevated;
		}
				
		public static List<string> getdll(){
			return DLL_NAMES;
		}
	}
}