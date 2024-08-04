using Newtonsoft.Json.Linq;

namespace FormulaEvaluator {
    public class Utility {
        private static string getSpecificValue(JToken valuejs, string path){
            if(path.Contains("dataset")){
                JObject o = valuejs.ToObject<JObject>();
                Console.WriteLine(o);
                Console.WriteLine(path);
                JToken res = o.SelectToken(path.Substring(0, path.IndexOf(']') + 1), true);
                string subPath = path.Substring(path.IndexOf(']') + 2);
                return res.ToObject<JObject>().ContainsKey(subPath) ? res[subPath].ToString() : "";
            } else {
                JObject o = valuejs.ToObject<JObject>();
                JToken res = o.SelectToken(path);
                return res.ToString();
            }
        }
        public static string getValueByPath(JToken valuejs, string path, string postfix = ""){
            if(path == "") return "";

            string[] c_path = path.Split(','); 
            string colname = c_path[c_path.Length - 1]; 
            for(int pathi = 0; pathi <  c_path.Length; pathi++)
            {
                if (c_path[pathi] != colname && pathi != 0) {
                    c_path[pathi] = c_path[pathi] + postfix;
                }
            }
            return getSpecificValue(valuejs, string.Join(".", c_path));
        }

        public static void setValueByPath(JToken valuejs,  JToken new_input, string path){
            if(path == "" || valuejs == null) return; 
            string[] c_path = path.Split('.'); 
            string colname = c_path[c_path.Length - 1]; 
            int len = c_path.Length;
            int out_new_input;
            new_input = !(new_input.Type.ToString() == "String") && int.TryParse(new_input.ToString(), out out_new_input) ? int.Parse(new_input.ToString()) : new_input;
            var parents = valuejs.SelectTokens(path.Substring(0, len - 1));
            foreach(var parent in parents){
                parent[colname] = new_input;
            }
        }

    }
}