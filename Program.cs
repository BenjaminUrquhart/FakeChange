using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace FakeChangePatcher
{
    internal class Program
    {
        public static readonly string ID_HEADER = "FAKECHANGE_PATCHED";

        public static readonly int VERSION = 1;
        public static readonly string VERSION_STRING = ID_HEADER + VERSION.ToString();

        static readonly string LAUNCH_PARAM_CODE = @"
        if (variable_global_exists(""__launch_params__""))
            return global.__launch_params__;
    
        var param_data = new launch_parameters();
    
        if (os_type == os_linux)
        {
            if (file_exists(game_save_id + ""/params""))
            {
                global.__launch_params__ = param_data;
                var buff = buffer_load(game_save_id + ""/params"");
                var length = buffer_get_size(buff);
                var out = """";
            
                while (buffer_tell(buff) < length)
                {
                    var param = buffer_read(buff, buffer_string);
                
                    if (param == ""launcher"")
                    {
                        param_data.is_launcher = true;
                    }
                    else if (string_pos(""switch_"", param) != 0)
                    {
                        var param_parts = string_split(param, ""_"");
                        param_data.switch_id = real(param_parts[1]);
                    }
                    else if (string_pos(""returning_"", param) != 0)
                    {
                        var param_parts = string_split(param, ""_"");
                        param_data.returning = real(param_parts[1]);
                    }
                
                    out += (param + "" "");
                }
            
                show_debug_message(out);
                file_delete(game_save_id + ""/params"");
                buffer_delete(buff);
            }
        
            return param_data;
        }
        ";

        static readonly string GAME_CHANGE_CODE = @"
        var parameters = get_chapter_switch_parameters();
        if (os_type == os_linux) {
                show_debug_message(""Requested chapter "" + string(arg0));
                show_debug_message(""Save folder: "" + game_save_id);
                var handle = external_define(""libfakechange.so"", ""fake_change"", 0, 0, 3, 1, 0, 1);
                external_call(handle, game_save_id, arg0, parameters);
                game_end();
                return;
        }
        ";

        // Applies to everything (including launcher)
        static readonly CodePatch[] GLOBAL_PATCHES = new CodePatch[] { 
            new(1, "gml_GlobalScript_scr_init_launch_parameters", "var param_data = new launch_parameters();", LAUNCH_PARAM_CODE)
        };

        static readonly Dictionary<uint, CodePatch[]> PER_CHAPTER_PATCHES = new Dictionary<uint, CodePatch[]>
        {
            { 3, new CodePatch[] { new(0, "gml_Object_obj_ch3_couch_video_Draw_0", "gpu_set_texfilter(true);", "") } }
        };

        static void Main(string[] args)
        {

            string folder = args.Length > 0 ? args[0] + Path.DirectorySeparatorChar : ".";

            if(!Directory.Exists(folder)) {
                Console.WriteLine("Folder not found: " + folder);
                Environment.Exit(1);
            }

            if(!Directory.Exists(folder + "chapter0"))
            {
                Console.WriteLine("Creating launcher folder...");
                Directory.CreateDirectory(folder + "chapter0");
                File.Move(folder + "data.win", folder + "chapter0/game.unx");
            }

            bool[] hasChapter = new bool[7];

            for(uint i = 1; i <= 7; i++)
            {
                string linFolder = folder + "chapter" + i.ToString();
                string winFolder = linFolder + "_windows";

                hasChapter[i - 1] = Directory.Exists(linFolder);

                if (!hasChapter[i - 1] && Directory.Exists(winFolder))
                {
                    Console.WriteLine("Moving chapter " + i.ToString() + " folder...");
                    Directory.Move(winFolder, linFolder);
                    hasChapter[i - 1] = true;
                }

                if (hasChapter[i - 1] && File.Exists(linFolder + "/data.win") && !File.Exists(linFolder + "/game.unx"))
                {
                    Console.WriteLine("Moving chapter " + i.ToString() + " data file...");
                    File.Move(linFolder + "/data.win", linFolder + "/game.unx");
                }

                //Console.WriteLine(i.ToString() + " -> " + hasChapter[i - 1].ToString() + " (" + winFolder + ")");
            }

            Console.WriteLine("Patching launcher...");
            PatchFile(folder + "chapter0/game.unx", "gml_Object_obj_CHAPTER_SELECT_Create_0", 0);

            for(uint i = 1; i <= 7; i++)
            {
                if (hasChapter[i - 1]) 
                {
                    Console.WriteLine("Patching chapter " + i.ToString() + "...");
                    PatchFile(folder + "chapter" + i.ToString() + "/game.unx", "gml_GlobalScript_scr_chapterswitch", i);
                }
            }
            Console.WriteLine("Done");
        }

        static void PatchFile(string filepath, string codename, uint chapter)
        {
            UndertaleData data;
            using (UndertaleReader reader = new UndertaleReader(File.OpenRead(filepath)))
            {
                data = reader.ReadUndertaleData();
            }

            int version = GetPatchVersion(data);

            if (version >= VERSION)
            {
                Console.WriteLine("Chapter " + chapter.ToString() + " already patched");
            }
            else
            {
                GlobalDecompileContext globalCtx = new GlobalDecompileContext(data);
                CodeImportGroup importer = new CodeImportGroup(data, globalCtx);

                if (version == -1)
                {
                    importer.QueueFindReplace(data.Code.ByName(codename), "var parameters = get_chapter_switch_parameters();", GAME_CHANGE_CODE);
                }
                else if (version == 0)
                {
                    importer.QueueFindReplace(data.Code.ByName(codename), "external_call(handle, game_save_id + \"/chapter\", arg0, parameters);", "external_call(handle, game_save_id, arg0, parameters);");
                }

                foreach (CodePatch patch in GLOBAL_PATCHES)
                {
                    if (version < patch.versionAdded)
                    {
                        Console.WriteLine("Applying patch to " + patch.targetEntry);
                        importer.QueueFindReplace(data.Code.ByName(patch.targetEntry), patch.targetCode, patch.code);
                    }
                }

                if (PER_CHAPTER_PATCHES.TryGetValue(chapter, out CodePatch[]? patches)) {
                    foreach(CodePatch patch in patches)
                    {
                        if(version < patch.versionAdded)
                        {
                            Console.WriteLine("Applying patch to " + patch.targetEntry);
                            importer.QueueFindReplace(data.Code.ByName(patch.targetEntry), patch.targetCode, patch.code);
                        }
                    }
                }

                Console.WriteLine("Finalizing...");
                importer.Import();

                MarkPatched(data);
                WriteDataFile(data, filepath);

                if (version == -1)
                {
                    Console.WriteLine("Chapter " + chapter.ToString() + " patched");
                }
                else
                {
                    Console.WriteLine("Chapter " + chapter.ToString() + " upgraded from " + version.ToString() + " to " + VERSION.ToString());
                }
            }
        }

        static int GetPatchVersion(UndertaleData data)
        {
            UndertaleString? existing = data.Strings.FirstOrDefault(s => s.Content.StartsWith(ID_HEADER));
            if(existing != null)
            {
                if (existing.Content != ID_HEADER && int.TryParse(existing.Content.AsSpan(ID_HEADER.Length), out int res))
                {
                    return res;
                }
                return 0;
            }

            return -1;
        }

        static void MarkPatched(UndertaleData data)
        {
            UndertaleString? existing = data.Strings.FirstOrDefault(s => s.Content.StartsWith(ID_HEADER));
            if(existing != null)
            {
                existing.Content = VERSION_STRING;
            }
            else
            {
                data.Strings.MakeString(VERSION_STRING);
            }
        }

        static void WriteDataFile(UndertaleData data, string path)
        {
            using (UndertaleWriter writer = new UndertaleWriter(File.OpenWrite(path)))
            {
                writer.WriteUndertaleData(data);
            }
        }
    }

    public struct CodePatch
    {
        public int versionAdded { get; }
        public string targetEntry { get; }
        public string targetCode { get; }
        public string code { get; }

        public CodePatch(int versionAdded, string targetEntry, string targetCode, string code)
        {
            this.versionAdded = versionAdded;
            this.targetEntry = targetEntry;
            this.targetCode = targetCode;
            this.code = code;
        }
    }
}
