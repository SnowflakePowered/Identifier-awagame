using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Snowflake.Plugin;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using System.ComponentModel.Composition;
using System.IO.Compression;
using Snowflake.Service;
using Snowflake.Identifier;
using Snowflake.Utility;
using System.Data.SQLite;

namespace Identifier.Awagame
{
    public sealed class IdentifierAwagame: BaseIdentifier
    {
        private string databasePath;
        [ImportingConstructor]
        public IdentifierAwagame([Import("coreInstance")] ICoreService coreInstance)
            : base(Assembly.GetExecutingAssembly(), coreInstance)
        {
            this.databasePath = Path.Combine(this.PluginDataPath, "awagame.db");
            this.ExtractDatabase();

        }

   
        private void ExtractDatabase()
        {
            if (!File.Exists(this.databasePath))
            {
                using (Stream stream = this.GetResource("awagame.zip"))
                using (ZipArchive zip = new ZipArchive(stream))
                {
                    try
                    {
                        zip.ExtractToDirectory(this.PluginDataPath);
                    }
                    catch
                    {
                        Console.WriteLine("Unable to extract awagame.zip");
                    }
                }
            }
        }

        public override string IdentifyGame(string fileName, string platformId)
        {
            return IdentifyGame(File.OpenRead(fileName), platformId);
        }
        public override string IdentifyGame(FileStream file, string platformId)
        {
            string md5 = Snowflake.Utility.FileHash.GetMD5(file);
            string match = null;
            file.Close();
            
            using(SQLiteConnection database = new SQLiteConnection("Data Source="+this.databasePath+";Version=3;"))
            using (SQLiteCommand command = new SQLiteCommand("SELECT `gamename` from `roms` where md5 = @md5"))
            {
                database.Open();
                command.Connection = database;
                command.Parameters.AddWithValue("@md5", md5);
                match = command.ExecuteScalar() as string;
                database.Close();
            }
     
            if (match == null) return null;
            //Magical regex that removes anything in square brackets and parentheses, taking out GoodTools and NoIntro tags
            //Also preserves common punctuation (+, -, ~, !, ?, ,, ;, $, %, ^, &, *, @, #, ", ') found in game titles.
            //Should be accurate for most cases
            var cleanMatch = Regex.Match(match, @"(\([^]]*\))*(\[[^]]*\])*([\w\+\~\@\!\#\$\%\^\&\*\;\,\'\""\?\-\.\-\s]+)");
      
            return cleanMatch.Groups[3].Value;
        }
    }
}
