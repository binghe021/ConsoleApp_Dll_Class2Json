using NLog;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ConsoleApp_Dll_Class2Json
{
    public class Program
    {
        static Logger logger = LogManager.GetLogger("logger");

        static string DllPath = ConfigurationManager.AppSettings["DllPath"].ToString();
        static string JsonPath = ConfigurationManager.AppSettings["JsonPath"].ToString();
        static string IsOnlyCheckPublicClass = ConfigurationManager.AppSettings["IsOnlyCheckPublicClass"].ToString();
        static string FiltersClassKeyName = ConfigurationManager.AppSettings["FiltersClassKeyName"].ToString();

        static void Main(string[] args)
        {
            LogInfo("------------------------Dll_Class2Json V1.0------------------------");
            LogInfo("本程序主要作用是扫描DLL文件夹下所有dll/exe的代码中的类及其属性清单并生成json文本文件。");
            LogInfo("读取配置文件中 DLL总文件夹 参数值");
            LogInfo(DllPath);
            LogInfo("读取配置文件中 生成的Json文件总文件夹 参数值");
            LogInfo(JsonPath);
            LogInfo("读取配置文件中 是否只处理Public可访问性的类 是Y则处理，其他不处理 参数值");
            LogInfo(IsOnlyCheckPublicClass);
            LogInfo("读取配置文件中 是否只处理包含某关键字的类 为空则不筛选，有值则筛选 参数值");
            LogInfo(FiltersClassKeyName);

            LogInfo("扫描开始");

            string[] dllFileList = new string[] { };
            string[] exeFileList = new string[] { };
            ShowDllsList(out dllFileList, out exeFileList);

            ScanDllsList(dllFileList, exeFileList);

            LogInfo("处理完成");
            LogInfo("Over...");
            //Console.ReadLine();
        }


        /// <summary>
        /// 显示清单
        /// </summary>
        /// <param name="dllFileList"></param>
        /// <param name="exeFileList"></param>
        public static void ShowDllsList(out string[] dllFileList, out string[] exeFileList)
        {
            LogInfo("获取DLL文件夹下所有dll/exe清单开始");


            dllFileList = Directory.GetFiles(DllPath, "*.dll", SearchOption.TopDirectoryOnly);
            LogGreen($"文件夹中包含dll文件{dllFileList.Length}个");
            if (dllFileList != null && dllFileList.Length > 0)
            {
                LogGreen($"dll总数：{dllFileList.Length}");
                LogGreen("----------------------------------");
                foreach (var dll in dllFileList)
                {

                    LogGreen("dll名称: " + dll);
                }

            }

            LogGreen("=======================================");


            exeFileList = Directory.GetFiles(DllPath, "*.exe", SearchOption.TopDirectoryOnly);
            LogGreen($"文件夹中包含exe文件{exeFileList.Length}个");
            if (exeFileList != null && exeFileList.Length > 0)
            {
                LogGreen($"exe总数：{exeFileList.Length}");
                LogGreen("----------------------------------");
                foreach (var exe in exeFileList)
                {

                    LogGreen("exe名称: " + exe);
                }

            }

            LogInfo("获取Dll文件夹下所有dll/exe清单结束");


        }

        /// <summary>
        /// 扫描DLL/EXE清单
        /// </summary>
        /// <param name="dllFileList"></param>
        /// <param name="exeFileList"></param>
        public static void ScanDllsList(string[] dllFileList, string[] exeFileList)
        {
            LogInfo("扫描DLL文件夹下所有dll/exe清单开始");

            if (dllFileList != null && dllFileList.Length > 0)
            {
                LogGreen("----------------------------------");
                foreach (var dll in dllFileList)
                {
                    LogGreen("dll名称: " + dll);
                    ScanDll(dll);
                }


            }

            LogGreen("=======================================");

            if (exeFileList != null && exeFileList.Length > 0)
            {
                LogGreen("----------------------------------");
                foreach (var exe in exeFileList)
                {
                    LogGreen("exe名称: " + exe);
                    ScanDll(exe);
                }


            }

            LogInfo("获取Dll文件夹下所有dll/exe清单结束");


        }



        /// <summary>
        /// 扫描DLL/EXE文件
        /// </summary>
        /// <param name="DllPath"></param>
        public static void ScanDll(string DllPath)
        {

            //若文件夹存在则先删除，之后再新建文件夹
            string jsonDirName = GetFileName(DllPath);
            jsonDirName = JsonPath + jsonDirName;
            if (Directory.Exists(jsonDirName))
            {
                string[] files = Directory.GetFiles(jsonDirName, "*.*");
                foreach (var file in files)
                {
                    File.Delete(file);
                }
                Directory.Delete(jsonDirName);
            }
            Directory.CreateDirectory(jsonDirName);

            LogGreen($"扫描DLL文件开始{DllPath}");

            try
            {
                //Assembly assembly = Assembly.LoadFile(DllPath);//在加载的dll引用了别的dll时会报错，不推荐使用。
                Assembly assembly = Assembly.LoadFrom(DllPath);

                Type[] types = assembly.GetTypes();
                if (!string.IsNullOrEmpty(FiltersClassKeyName))
                {
                    types = types.Where(q => q.IsClass && q.Name.Contains(FiltersClassKeyName)).ToArray();
                }
                if (IsOnlyCheckPublicClass.ToUpper() == "Y")
                {
                    types = types.Where(q => q.IsClass && q.IsPublic == true).ToArray();
                }

                foreach (Type type in types)
                {
                    WriteJson(type, jsonDirName);
                }

            }
            catch (Exception ex)
            {
                LogYellow($"读取DLL未成功，可能不是.net开发的DLL。{DllPath}----" + ex.Message);
            }

            LogInfo("扫描DLL文件结束");
        }



        public static void WriteJson(Type type, string jsonDirName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");

            LogInfo(type.FullName);

            //获取所有属性集合
            PropertyInfo[] propertyInfos = type.GetProperties();
            foreach (var prop in propertyInfos)
            {
                string propName = prop.Name;
                TypeCode propTypeCode = Type.GetTypeCode(prop.PropertyType);
                switch (propTypeCode)
                {
                    case TypeCode.Empty:
                        sb.Append($"\"{propName}\": \"0\",");
                        break;
                    case TypeCode.Object:
                        sb.Append("\"" + propName + "\": {},");//引用的另一个对象
                        break;
                    case TypeCode.DBNull:
                        sb.Append($"\"{propName}\": \"2\",");
                        break;
                    case TypeCode.Boolean:
                        sb.Append($"\"{propName}\": true,");
                        break;
                    case TypeCode.Char:
                        sb.Append($"\"{propName}\": \"a\",");
                        break;
                    case TypeCode.SByte:
                        sb.Append($"\"{propName}\": 5,");
                        break;
                    case TypeCode.Byte:
                        sb.Append($"\"{propName}\": 6,");
                        break;
                    case TypeCode.Int16:
                        sb.Append($"\"{propName}\": 16,");
                        break;
                    case TypeCode.UInt16:
                        sb.Append($"\"{propName}\": 16,");
                        break;
                    case TypeCode.Int32:
                        sb.Append($"\"{propName}\": 32,");
                        break;
                    case TypeCode.UInt32:
                        sb.Append($"\"{propName}\": 32,");
                        break;
                    case TypeCode.Int64:
                        sb.Append($"\"{propName}\": 64,");
                        break;
                    case TypeCode.UInt64:
                        sb.Append($"\"{propName}\": 64,");
                        break;
                    case TypeCode.Single:
                        sb.Append($"\"{propName}\": 13.1,");
                        break;
                    case TypeCode.Double:
                        sb.Append($"\"{propName}\": 14.12,");
                        break;
                    case TypeCode.Decimal:
                        sb.Append($"\"{propName}\": 15.12,");
                        break;
                    case TypeCode.DateTime:
                        sb.Append($"\"{propName}\": \"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\",");
                        break;
                    case TypeCode.String:
                        sb.Append($"\"{propName}\": \"string\",");
                        break;
                    default:
                        break;
                }

            }

            string tmp = sb.ToString();
            tmp = tmp.TrimEnd(',');//移除最后一个逗号，

            sb.Clear();
            sb.Append(tmp);
            sb.AppendLine("\r}");

            //保存为具体json文件
            string jsonFile = jsonDirName + "\\" + type.Name + ".json";
            File.WriteAllText(jsonFile, sb.ToString(), Encoding.UTF8);

        }


        /// <summary>
        /// 从文件全路径中提取出文件名
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <returns></returns>
        public static string GetFileName(string fileFullName)
        {
            string resultFileName = "";
            if (string.IsNullOrEmpty(fileFullName))
            {
                return "";
            }
            else
            {

                resultFileName = fileFullName.Substring(fileFullName.LastIndexOf("\\") + 1);
                resultFileName = resultFileName.Substring(0, resultFileName.LastIndexOf("."));
            }
            return resultFileName;
        }

        /// <summary>
        /// 默认颜色
        /// </summary>
        /// <param name="Info"></param>
        public static void LogInfo(string Info)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Info);
            logger.Info(Info);
        }

        /// <summary>
        /// 前景色变为绿色 着重显示
        /// </summary>
        /// <param name="Find"></param>
        public static void LogGreen(string Find)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(Find);
            logger.Info(Find);
        }

        /// <summary>
        /// 前景色变为黄色 着重显示
        /// </summary>
        /// <param name="Find"></param>
        public static void LogYellow(string Find)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Find);
            logger.Error(Find);
        }

    }

}
