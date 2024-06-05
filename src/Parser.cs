using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;

namespace FormulaEvaluator {
    public class Parser {
        List<(Token, object? v)> Tokens;
        int pos;

        

        public class Expr {
            double Val;
            string? SVal;
            public Expr(){}
            public Expr(double val){
                Val = val;
            }
            public Expr(string? val){
                SVal = val;
            }
            virtual public void Print() {
                if(SVal != null){
                    Console.WriteLine(SVal);
                } else {
                    Console.WriteLine(Val);
                }
            }

            virtual public object? Eval(JToken source,JObject storage) {
                if(SVal != null && storage.ContainsKey(SVal)) {
                    //Console.WriteLine("SVal : {0}, Storage[SVal]: {1}",SVal,storage[SVal]?.ToString());
                    return storage[SVal].ToString();
                } else if(SVal != null && !storage.ContainsKey(SVal)){
                    storage[SVal] = 0; 
                    return 0;
                } else if(SVal == null) {
                   return Val; 
                }
                return SVal;
            }
            public  double GetValue(){
                return Val;
            }
            public string GetSValue(){
                return SVal == null ? "" : SVal;
            }
            
        } 

        public class PathExpr: Expr {
            string? Path;
            Expr    ValExpr;
            public PathExpr(string path, Expr val){
                Path = path;
                ValExpr  = val;
            }
            public override object? Eval(JToken source, JObject storage)
            {
                object? vExpr = ValExpr != null ? ValExpr.Eval(source, storage) : null; 
                string[] paths = Path?.ToString().Split('.');
                string colName = paths[paths.Length - 1]; 
                if(ValExpr == null){
                    if(paths[0].Contains("query_")) {
                        if(storage.ContainsKey(paths[0])){
                            if(paths.Length > 2) {
                                return storage[paths[0]][paths[paths.Length - 2].ToString()][colName];
                            } else {
                                return storage[paths[0]][colName];
                            }
                            
                        }
                    } else {
                        return getValueByPath(source, Path.ToString());
                    }
                } else {
                    JToken input = (vExpr?.GetType() == typeof(String) ? vExpr?.ToString() : double.Parse(vExpr != null ? vExpr.ToString() : "0"));
                    if(paths[0].Contains("_query")){
                        setValueByPath(storage, input, Path);
                        /*if(storage.ContainsKey(paths[0])){ 
                            storage[paths[0]][paths[paths.Length - 2].ToString()][colName] = vExpr.GetType() == typeof(String) ? vExpr.ToString() : double.Parse(vExpr.ToString());
                        }*/
                    } else {
                         setValueByPath(source, input, Path);
                        /*string colname = paths[paths.Length - 1]; 
                        if(paths.Length > 2) {
                            if(source.ToObject<JObject>().ContainsKey(paths[paths.Length - 2] + "_SUBFORM")){
                                if(source[paths[paths.Length - 2] + "_SUBFORM"].Count() > 1){
                                    for(int k = 0; k < source[paths[paths.Length - 2] + "_SUBFORM"].Count();k++){
                                        source[paths[paths.Length - 2] + "_SUBFORM"][k][colname] = vExpr.GetType() == typeof(String) ? vExpr.ToString() : double.Parse(vExpr.ToString());
                                    }   
                                } else if(source[paths[paths.Length - 2] + "_SUBFORM"].Count() == 1) {
                                    source[paths[paths.Length - 2] + "_SUBFORM"].FirstOrDefault()[colname] = vExpr?.GetType() == typeof(String) ? vExpr?.ToString() : double.Parse(vExpr != null ? vExpr.ToString() : "0");
                                }  
                            }
                        } else {
                            if (colname == "") return "";
                            source[colname] = vExpr.GetType() == typeof(String) ? vExpr.ToString() : double.Parse(vExpr.ToString());
                            
                        }*/
                    }
                }
                return null;
            } 
        }
        public class NumberExpr: Expr {
            double Val;            
        
            public NumberExpr(double val){
                this.Val = val;
            }
            public override object? Eval(JToken source,JObject storage) { 
                 
                return Val;
            }
            
        }
        public class StringExpr: Expr {
            string? Val; 
            public StringExpr(string val){
                Val = val;
            }
            public override object? Eval(JToken source,JObject storage) {  
                return Val;
            }
        }

        public class VarExpr: Expr {
            enum VarType {
                STRING_TYPE,
                DOUBLE_TYPE,
            };
            string VarName; 
            Expr   ValExpr;           
        
            public VarExpr(string name, Expr e) {
                VarName = name;
                ValExpr = e;
            }
        
            public override object? Eval(JToken source,JObject storage)
            {
                object? vExpr = ValExpr != null ? ValExpr.Eval(source, storage) : null;
                if(!storage.ContainsKey(VarName)){
                    storage[VarName] = vExpr != null ? vExpr.ToString() : "";
                }
                if(ValExpr == null){
                    return storage[VarName.ToString()].ToString();
                } else {
                    storage[VarName] = vExpr.GetType() == typeof(String) ? vExpr.ToString() : double.Parse(vExpr?.ToString());
                    return vExpr;
                }
            } 
        }

        public class BinaryExpr: Expr {
            Expr lhs;
            Expr rhs;
            Token op;
            public BinaryExpr(Expr lhs, Token op, Expr rhs){
                this.lhs = lhs;
                this.rhs = rhs;
                this.op = op;
            }

            public override void Print() {
                if(lhs != null){
                    lhs.Print();
                }
                
                Console.WriteLine(op);
                if(rhs != null){
                    rhs.Print();
                } 
            }

            public override object? Eval(JToken source,JObject storage)
            {
                object? l_object = lhs?.Eval(source, storage);
                object? r_object = rhs?.Eval(source, storage);
                bool is_string_type = false;
                double outVal;

                if(l_object?.GetType() == typeof(String)
                    && (!double.TryParse(l_object?.ToString(), out outVal) || (l_object.ToString()[0] == '0' && l_object.ToString().Length > 1)) && !l_object.ToString().Contains(".")){       
                  is_string_type = true;
                } 
                switch (op) { 
                    case Token.Div: {   
                        return is_string_type ? l_object : double.Parse(l_object?.ToString()) / double.Parse(r_object.ToString());
                    }
                    case Token.Mul:  {    
                        return is_string_type ? l_object : double.Parse(l_object?.ToString()) * double.Parse(r_object.ToString());
                    }
                    case Token.Plus: { 
                        return is_string_type && l_object?.ToString() != "" ? l_object?.ToString() + r_object?.ToString() : double.Parse(l_object?.ToString() == "" ? "0" : l_object?.ToString()) + double.Parse(r_object?.ToString() == "" ? "0" : r_object?.ToString());
                    }
                    case Token.Minus: {
                        Console.WriteLine(l_object?.ToString());
                        
                        return is_string_type ? l_object : double.Parse(l_object.ToString()) - double.Parse(r_object.ToString()); 
                    }
                    case Token.Mod: {
                        return is_string_type ? l_object : double.Parse(l_object?.ToString()) % double.Parse(r_object.ToString());
                    }
                }
                return null;
            }

            public Token GetOp() {
                return op;
            }

            public Expr GetLhs() {
                return lhs;
            }
            public Expr GetRhs() {
                return rhs;
            }
        }


        public Expr ParseVar(){
            string varName = this.Tokens[this.pos].v?.ToString();
            
            Consume(Token.Var);
            if(this.Tokens[this.pos].Item1 == Token.Equal) {
                Consume(Token.Equal);
                Expr rhs = ParseExpr();
                
                return new VarExpr(varName, rhs);
            }
           return new VarExpr(varName, null); 
        }

        public Expr ParsePrimary(){
            var current_token = this.Tokens[this.pos].Item1;
            switch (current_token){
                case Token.LParen: {
                    return ParseParen();
                }
                case Token.Number:{
                    Expr e = new NumberExpr(double.Parse(this.Tokens[this.pos].v.ToString()));
                    
                    Consume(Token.Number);
                    return e;
                }
                case Token.String: {
                    Expr e = new StringExpr(this.Tokens[this.pos].v.ToString());
                    Consume(Token.String);
                    return e;
                }
                case Token.Var: {
                    return ParseVar();
                }
                case Token.Path: {
                    string path = this.Tokens[this.pos].v.ToString();
                    Consume(Token.Path);
                    if(this.Tokens[this.pos].Item1 == Token.Equal) {
                        Consume(Token.Equal);
                        Expr rhs = ParseExpr();

                        return new PathExpr(path, rhs);
                        
                    }
                    return new PathExpr(path, null);
                }
            } 
            return null;
        } 
        public Expr ParseBinaryMul(){
            Expr lhs = ParsePrimary();
            var current_token = this.Tokens[this.pos].Item1;
            switch(current_token) {
                case Token.Div:{
                    Consume(current_token);
                    Expr rhs = ParseBinaryMul();
                    return new BinaryExpr(lhs, current_token, rhs);
                }
                
                case Token.Mul:{
                    Consume(current_token);
                    Expr rhs = ParseBinaryMul();
                    return new BinaryExpr(lhs, current_token, rhs);
                }
                default: break;
            }
            return lhs;
        }
       
        public Expr ParseBinaryPlus(){
            Expr lhs = ParseBinaryMul();
            var current_token = this.Tokens[this.pos].Item1;
            switch(current_token) {
                case Token.Plus:{
                    Consume(Token.Plus);
                    Expr rhs = ParseBinaryPlus();
                    return new BinaryExpr(lhs, current_token, rhs);
                }
                
                case Token.Minus:{
                    Consume(Token.Minus);
                    Expr rhs = ParseBinaryPlus();
                    return new BinaryExpr(lhs, current_token, rhs);
                }
                default: break;
            }
            return lhs;  
        }
        public Expr ParseExpr(){
            return ParseBinaryPlus();
        }
        public Expr ParseParen(){
            Consume(Token.LParen);
            Expr current = ParseExpr();
            
            Consume(Token.RParen);
            return current;
        }
        public void Consume(Token tok) {
            if(this.Tokens[this.pos].Item1 == tok) {
                pos++;
            } else {
                
                throw new Exception("Expected "+ tok + " Found instead " + this.Tokens[this.pos].Item1);
            }
        }

        public Parser(string input){
            this.Tokens = Lexer.ParseTokens(input).ToList();
            this.pos = 0;
        }

        public void Parse(JToken source, JObject storage){
            while(this.pos < this.Tokens.Count()){  
                
                Expr root = ParseExpr();
                Console.WriteLine(root?.Eval(source, storage));
                 
                pos++;
            }
        }

        public static string getValueByPath(JToken valuejs, string path){
            if(path == "") return "";
            string[] c_path = path.Split('.'); 
            string colname = c_path[c_path.Length - 1]; 
            JObject valueObject = valuejs.ToObject<JObject>();
            int len = c_path.Length;
            if(len == 1){
                if(valueObject.ContainsKey(colname)){
                    return valuejs[colname].ToString();
                } else {
                    return "";
                }
            } 
            if(len > 2) {
                if(valuejs.ToObject<JObject>().ContainsKey(c_path[c_path.Length - 2] + "_SUBFORM")){
                    return valuejs[c_path[c_path.Length - 2] + "_SUBFORM"].FirstOrDefault()[colname].ToString();
                }
            } else { // we enter if len == 2
                if (colname == "") return "";
                if(valueObject.ContainsKey(c_path[len - 2])){
                    return valuejs[c_path[len - 2]][colname].ToString();
                } 
                
                if(valueObject.ContainsKey(colname)){
                    return valuejs[colname].ToString();
                }
            }

            return "";
        }

        public static void setValueByPath(JToken valuejs,  JToken new_input, string path){
            if(path == "") return;
            string[] c_path = path.Split('.'); 
            string colname = c_path[c_path.Length - 1]; 
            JObject valueObject = valuejs.ToObject<JObject>();
            int len = c_path.Length;
            if(len == 1){
                if(valueObject.ContainsKey(colname)){
                    valuejs[colname] = new_input;
                } else {
                    return;
                }
            } 
            if(len > 2) {
                if(valueObject.ContainsKey(c_path[c_path.Length - 2] + "_SUBFORM")){
                    if(valuejs[c_path[c_path.Length - 2] + "_SUBFORM"].Count() > 1){
                        for(int k = 0; k < valuejs[c_path[c_path.Length - 2] + "_SUBFORM"].Count();k++){
                            valuejs[c_path[c_path.Length - 2] + "_SUBFORM"][k][colname] = new_input;
                        }   
                    } else if(valuejs[c_path[c_path.Length - 2] + "_SUBFORM"].Count() == 1) {
                        valuejs[c_path[c_path.Length - 2] + "_SUBFORM"].FirstOrDefault()[colname] = new_input;
                    } 
                    
                }
            } else { // we enter if len == 2
                if (colname == "") return;
                if(valueObject.ContainsKey(c_path[len - 2])){
                    valuejs[c_path[len - 2]][colname] = new_input;
                } 
                
                if(valueObject.ContainsKey(colname)){
                    valuejs[colname] = new_input;
                }
            }
        }
    }
}