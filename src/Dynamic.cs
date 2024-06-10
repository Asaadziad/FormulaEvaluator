namespace FormulaEvaluator {
    public class Dynamic {
    public static string GetDynamicValue(string path) {
            string[] _path = path.Contains(',') ? path.Split(',') : path.Split('.');
            string type = _path[_path.Length - 2];
            string value_type = _path[_path.Length - 1];
            if(_path.Length < 1){
                return null;
            }

            switch(type){
                case "DATE":  
                return Dates.GetDynamicDate(value_type);
            }

            return null;
        }
    }
}