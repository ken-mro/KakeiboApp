using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KakeiboApp.Repository;

public class Constants
{
    private const string DATABSE_NAME = "KakeiboApp.db3";
    public static string DataBasePath => Path.Combine(FileSystem.AppDataDirectory, DATABSE_NAME);
}
