using System;
using System.Net;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO.Compression;
using System.Diagnostics;
using Ionic.Zip;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Threading;
using System.Management;
using OsInfo;


namespace ftsecuritysf
{
	class Deploy
	{

		static string HOST = get_hostname();
		static string INSTALL_PATH = choose_install_path();
		static string SPLUNK_INSTALL_PATH = INSTALL_PATH+"\\SplunkUniversalForwarder";
		static string SPLUNK_APP_PATH = INSTALL_PATH+"\\SplunkUniversalForwarder\\etc\\apps";
		static string SPLUNK_LOCAL_PATH = INSTALL_PATH+"\\SplunkUniversalForwarder\\etc\\system\\local";
		static string SPLUNK_INPUTS = SPLUNK_APP_PATH+"[]\\inputs.conf";
		static string SPLUNK_PROPS = SPLUNK_APP_PATH+"[]\\props.conf";
		static string MSI = string.Format("splunkforwarder-6.5.0-{0}.msi",get_arch());
		static string ZIP = "[].zip";
		static string SOURCE_TYPE = "spunk_deployment";
		static string INPUT_TYPE = "uber_prod";
		static string LOG_CONFIG = "##\n## Standards logs\n##\n[default]\nhost = {0}\nindex = {1}\n_meta = systemCode::{2}\n\n[monitor://C:\\logs\\]\nwhitelist = \\.log$\ndisabled = false\nindex=app_prod\nignoreOlderThan = 1d";
		static string NAMESPACE = "ftsecuritysf.Resources.";
		static IDictionary<string, string> KV_OBJ= new Dictionary<string,string>();
		//static string REGISTRY_PATH = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";

		// file to delete
		public static List<string> _fileList = new List<string>();


		public static void Sfdeploy ()
		{
			Deploy Start = new Deploy ();

			// Start logging
			Console.WriteLine ("Program started.");
			Start.EvtLog ();

			double vers = 5.2;
			bool IsOld = false;
			bool IsInst = false;


			// Don't check on WMI if the version is under 5.2
			if (checkwindowsversion (vers)) {
				IsOld = true;
			}

			if (!IsOld) {
				IsInst = IsProgramInstalled ("universalforwarder");
			}
			// Check if already installed
			if (!IsInst) {
				// Install splunk
				Console.WriteLine ("Install of splunk forwarder processing...");
				Start.Launch ();
			} else {
				string mess = "Splunk forwarder has already been installed.";
				Console.WriteLine (mess);
				_Logger.WriteEntry (mess);

			}
			
			// configure splunk
			Console.WriteLine("Configuration of splunk forwarder processing...");
			Start.Config();

			// Splunk logging
			KV_OBJ["arch"] = get_arch();
			KV_OBJ["install_path"] = SPLUNK_INSTALL_PATH;

			string stringToLog = splunkLogger (KV_OBJ);
			_Logger.WriteEntry (stringToLog);

			// removing files
			Thread.Sleep(2000);
			Start.Deletefiles(_fileList);

		}
			
		private static EventLog _Logger;

		public void EvtLog() {
			_Logger = new EventLog("Application");
			_Logger.Source = SOURCE_TYPE;
		}

		public static bool is64bit(){

			bool is64bit = !string.IsNullOrEmpty(
				Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"));

			return is64bit;

		}

		public static string get_arch() {
			string architecture = "";
			bool arch = is64bit();
			if (arch) {
				architecture = "x64";
			}
			else {
				architecture = "x86";
			}
			return architecture;
		}

		public static string get_hostname () {
			return System.Environment.MachineName;
		}

		public static string install_path_x86 () {
			string env = Environment.GetEnvironmentVariable ("commonprogramfiles");
			if (!File.Exists(env)){
				env = "C:\\Program Files\\Common Files";
			}
			return env;
		}

		public static string install_path_x64 () {
			string env = Environment.GetEnvironmentVariable ("commonprogramfiles(x86)");
			if (!File.Exists(env)){
				env = "C:\\Program Files (x86)\\Common Files";
			}
			return env;
		}
			
		//	public string get_systemCode(HOST) {
		//			string default_systemCode = "unkown";
		//			// CMDB call retrieve system code
		//			//  string output = get_system_code(HOST)
		//			if (output != default_systemCode) {
		//				string systemCode = output;
		//			} else {
		//				string systemCode = default_systemCode;
		//			}
		//
		//			
		//			return systemCode;
		//		}


		private static string choose_install_path() {
			string install_path = "";
			bool arch = is64bit();
			if (arch) {
				install_path = install_path_x64();
			} else {
				install_path = install_path_x86();
			}
			install_path = install_path_x64();
			return install_path;
		}

		// deleting files
		public void Deletefiles (List<string> files){
			foreach (string file in files){
				if (File.Exists(file)) {
					File.Delete(file);
				}
			}
		}

		// extract ZIP
		public void ExtractZip(string zipfile,string destpath){
			var zip = new ZipFile ();
			zip = ZipFile.Read (zipfile);
			zip.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
			zip.ExtractAll (destpath);
			zip.Dispose();
		}

		// check if software installed
		public static bool IsProgramInstalled(string programDisplayName) {
			ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_Product");
			foreach(ManagementObject mo in mos.Get()) {
				if (mo ["Name"].ToString ().ToLower () == programDisplayName) {
					return true;
				}
			}
			return false;
		}
			

		// start CMD
		private static int start_cmd(string cmd, string args){
			System.Diagnostics.Process installerProcess = new System.Diagnostics.Process ();
			installerProcess.StartInfo.FileName = cmd;
			installerProcess.StartInfo.Arguments = args;
			installerProcess.StartInfo.UseShellExecute = false;
			installerProcess.Start ();
			while (installerProcess.HasExited == false) {
				//indicate progress to user
				//Application.DoEvents();
				System.Threading.Thread.Sleep (100);
			}
			int exitcode = installerProcess.ExitCode;
			return exitcode;
		}

		public void copy_file(string sourceFile, string destFile) {
			System.IO.File.Copy(sourceFile, destFile, true);
		}

		public void file_create(string filepath){
			if(!File.Exists(filepath)){
				File.Create (filepath).Close();
			}
		}

		private static string splunkLogger(IDictionary<string, string> kvObj){
			string logString = "";

			foreach (KeyValuePair<string, string> kvp in kvObj)
			{
				logString += string.Format("{0}=\"{1}\" ", kvp.Key, kvp.Value);
			}
			return logString;
		}

		public static bool checkwindowsversion(double vers){
			string win = System.Environment.OSVersion.Version.ToString();
			string win2003 = win.Substring (0, 3);
			if(Convert.ToDecimal(win2003) <= Convert.ToDecimal(vers)) {
				return true;
			}
			return false;
		}

		// Splunkforwarder deplosyment function
		private void Launch() {
			double vers = 6.0; ;
			// check windows version
			if (checkwindowsversion(vers)) {
				MSI = string.Format("splunkforwarder-NT60-{0}.msi",get_arch());
			}

			// extract splunkforwarder msi from exe
			KV_OBJ["msi"] = MSI;
			main.WriteResourceToFile (NAMESPACE+MSI,MSI);

			_fileList.Add (MSI);

			string splunk_args = string.Format("/i {0} INSTALLDIR=\"{1}\" AGREETOLICENSE=Yes WINEVENTLOG_APP_ENABLE=1 WINEVENTLOG_SEC_ENABLE=1 WINEVENTLOG_SYS_ENABLE=1 WINEVENTLOG_FWD_ENABLE=1 WINEVENTLOG_SET_ENABLE=1 REGISTRYCHECK_U=1 REGISTRYCHECK_BASELINE_U=1 REGISTRYCHECK_LM=1 REGISTRYCHECK_BASELINE_LM=1 WMICHECK_CPUTIME=1 WMICHECK_LOCALDISK=1 WMICHECK_FREEDISK=1 WMICHECK_MEMORY=1 /q ",MSI,SPLUNK_INSTALL_PATH);
			int exitcode = start_cmd("msiexec",splunk_args);

			if (exitcode == 0) {
				string mess = "Splunk forwarder has been properly installed in this server";
				Console.WriteLine (mess);
				_Logger.WriteEntry (mess);
				KV_OBJ["install_satus"] =  "SUCCESS";
			} else {
				string mess = "somthing went wrong during the installation of Splunk forwarder";
				Console.WriteLine (mess);
				_Logger.WriteEntry (mess);
				KV_OBJ["install_satus"] =  "FAILED";
			}

		}

		// Splunkforwarder configuration
		private void Config() {

			// extract resource from exe
			main.WriteResourceToFile (NAMESPACE + ZIP, ZIP);

			// decompress resource
			ExtractZip (ZIP, SPLUNK_APP_PATH);
			_fileList.Add(ZIP);

			// Get systemCode
			// string systemCode = get_systemCode();
			string systemCode = "unknown"; // waiting for CMDB API

			// Add SystemCode and FT standard logs path
			string input_str = string.Format (LOG_CONFIG, HOST.ToLower (), INPUT_TYPE, systemCode);

			file_create (SPLUNK_LOCAL_PATH + "\\" + "inputs.conf");
			File.AppendAllText (SPLUNK_LOCAL_PATH + "\\" + "inputs.conf", input_str + Environment.NewLine);

			copy_file (SPLUNK_PROPS, SPLUNK_LOCAL_PATH + "\\" + "props.conf");

			start_cmd ("cmd.exe", "/c net stop SplunkForwarder");
			int exitcode = start_cmd ("cmd.exe", "/c net start SplunkForwarder");

			if (exitcode == 0) {
				string mess = "Splunk forwarder has been properly updated";
				Console.WriteLine (mess);
				_Logger.WriteEntry (mess);
				KV_OBJ ["update_satus"] = "SUCCESS";
			} else {
				KV_OBJ ["update_satus"] = "FAILED";
			}

			KV_OBJ["systemCode"] = systemCode;
			KV_OBJ["host"] = HOST.ToLower();
			KV_OBJ["zip_file"] = ZIP;
		}

	}
		
}
