using System.Reflection.Metadata.Ecma335;

namespace FormulaEvaluator {
    public class Api {
        

        Dictionary<string, string> operations = new Dictionary<string, string>{
            {"+" , "<p>Addtion</p>"},
            {"-" , "<p>Minus</p>"},
            {"*" , "<p>Mult</p>"},
            {"/" , "<p>Divide</p>"},
            {"%" , "<p>Modulo</p>"}, 
        };

        string[] DynamicValuesTypes = new string[1] { "DATE" };
        public string GetOperations() {
            return operations.ToString(); 
        }

        public string GetDynamicValuesTypes(){
            return DynamicValuesTypes.ToString();
        }
        
        
    
    }
}