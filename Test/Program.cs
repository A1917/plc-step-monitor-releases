using System;
using System.Reflection;
using System.Windows.Forms;

namespace Test
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // 单文件发布：从嵌入资源加载 HslCommunication.dll（避免外部 dll 依赖）
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Frm_Main());
        }

        /// <summary>从嵌入资源解析 HslCommunication 程序集（单文件运行）</summary>
        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string name = new AssemblyName(args.Name).Name;
            if (name != "HslCommunication")
            {
                return null;
            }
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Test.HslCommunication.dll"))
            {
                if (stream == null)
                {
                    return null;
                }
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                return Assembly.Load(data);
            }
        }
    }
}
