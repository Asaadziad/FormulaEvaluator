using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

//using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;

namespace FormulaEvaluator {
    public class Parser {
        List<(Token, object v)> Tokens;
        int pos;

        

        public class Expr {
            double Val;
            string SVal;
            public Expr(){}
            public Expr(double val){
                Val = val;
            }
            public Expr(string val){
                SVal = val;
            }
            virtual public void Print() {
                if(SVal != null){
                    Console.WriteLine(SVal);
                } else {
                    Console.WriteLine(Val);
                }
            }
            
            virtual public object Eval(JToken source,JObject storage) {
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
            string Path;
            Expr    ValExpr;
            public PathExpr(string path, Expr val){
                Path = path;
                ValExpr  = val;
            }
            public override object Eval(JToken source, JObject storage)
            {
                object vExpr = ValExpr != null ? ValExpr.Eval(source, storage) : null; 
                string[] paths = Path?.ToString().Split('.');
                 
                if(ValExpr == null){
                    //Console.WriteLine(paths[0]);
                    if(paths[0].Contains("query_") || paths[0].Contains("var_") || paths[0].Contains("dataset_")) {
                        Console.WriteLine("i get it from storage");

                        return getValueByPath(storage, Path.ToString()); 
                    } else {
                        if(Path.Contains("DYNAMIC")){
                           
                            return Dynamic.GetDynamicValue(Path);
                        }
                        return getValueByPath(source, Path.ToString());
                    }
                } else {
                    JToken input = vExpr.GetType() == typeof(String) ? vExpr.ToString() : double.Parse(vExpr != null ? vExpr.ToString() : "0").ToString();
                    if(paths[0].Contains("query_") || paths[0].Contains("var_") || paths[0].Contains("dataset_")){          
                        setValueByPath(storage, input, Path);
                    } else {
                        setValueByPath(source, input, Path);
                    }
                    return string.Format("Successfully set column {0} with value {1}", paths.Last(), input);
                }
                
            } 
        }
        public class NumberExpr: Expr {
            double Val;            
        
            public NumberExpr(double val){
                this.Val = val;
            }
            public override object Eval(JToken source,JObject storage) { 
                 
                return Val;
            }
        }
        public class StringExpr: Expr {
            string Val; 
            public StringExpr(string val){
                Val = val;
            }
            public override object Eval(JToken source,JObject storage) {  
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
        
            public override object Eval(JToken source,JObject storage)
            {
                object vExpr = ValExpr != null ? ValExpr.Eval(source, storage) : null;
                if(!storage.ContainsKey(VarName)){
                    storage[VarName] = vExpr != null ? vExpr.ToString() : "";
                }
                if(ValExpr == null){
                    return storage[VarName.ToString()].ToString();
                } else {
                    storage[VarName] = vExpr.GetType() == typeof(String) ? vExpr.ToString() : double.Parse(vExpr.ToString()).ToString();
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

            public override object Eval(JToken source,JObject storage)
            {
                
                object l_object = lhs.Eval(source, storage);
                object r_object = rhs.Eval(source, storage);
                bool is_string_type = false;
                double outVal;
                if(l_object == null || r_object == null) return null;
                Console.WriteLine(l_object);
                Console.WriteLine(r_object);
                if(l_object.GetType() == typeof(String)
                    && (!double.TryParse(l_object.ToString(), out outVal) || (l_object.ToString()[0] == '0' && l_object.ToString().Length > 1)) && !l_object.ToString().Contains(".")){       
                  is_string_type = true;
                }
                object res; 
                switch (op) { 
                    case Token.Div: {   
                        try {
                            res =  is_string_type ? l_object : double.Parse(l_object.ToString() == "" ? "0" : l_object.ToString()) / double.Parse(r_object.ToString() == "" ? "0" : r_object.ToString());
                            return res;
                        } catch(Exception e){
                            return e.Message;
                        }
                       

                    }
                    case Token.Mul:  {    
                        try{
                            res =  is_string_type ? l_object : double.Parse(l_object.ToString() == "" ? "0" : l_object.ToString()) * double.Parse(r_object.ToString() == "" ? "0" : r_object.ToString());
                            return res;
                        } catch(Exception e){
                            return e.Message;
                        } 
                    }
                    case Token.Plus: {
                        try {
                            res =  is_string_type && l_object.ToString() != "" ? l_object.ToString() + r_object.ToString() : double.Parse(l_object.ToString() == "" ? "0" : l_object.ToString()) + double.Parse(r_object.ToString() == "" ? "0" : r_object.ToString()).ToString();
                                if (is_string_type)
                                {
                                    res = Regex.Replace(res.ToString(), "\t", " ");
                                }
                            return res;
                        }  catch(Exception e){
                            return e.Message;
                        }                     
                        
                    }
                    case Token.Minus: { 
                        try
                        {
                           res = is_string_type ? l_object : double.Parse(l_object.ToString() == "" ? "0" : l_object.ToString()) - double.Parse(r_object.ToString() == "" ? "0" : r_object.ToString()); 
                           return res;    
                        }
                        catch (Exception e)
                        {
                            
                            return e.Message;
                        }
                        
                    }
                    case Token.Mod: {
                        try {

                            res = is_string_type ? l_object : double.Parse(l_object.ToString() == "" ? "0" : l_object.ToString()) % double.Parse(r_object.ToString() == "" ? "0" : r_object.ToString());
                            return res;
                        }catch(Exception e){
                            return e.Message;
                        }
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
            string varName = this.Tokens[this.pos].v.ToString();
            
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

        public string Parse(JToken source, JObject storage){ 
            Expr root = ParseExpr();
            return root.Eval(source, storage).ToString();
        }

        public static int FindLatsetArray(string path)
        {
            string[] arrPath = path.Split('.');
            int len = arrPath.Length;
            bool[] indexes = new bool[len];
            int current = 0;
            foreach(var p in arrPath)
            {
                if (p.Contains('['))
                {
                    indexes[current] = true;
                }
                current++;
            }
            int latest = 0;
            for(int j = 0;j <  indexes.Count(); j++)
            {
                if (indexes[j])
                {
                    latest = j;
                }
            }
            return latest;
        }
        public static string GetSpecificValue(JToken valuejs, string path){
            JToken jobj = valuejs;
            if (!(path.Contains('.') || path.Contains('[')))
                return "";
            else{
                string[] arrPath = path.Split('.');
                int i = 0;
                string location = "";
                string num = "";
                int latest_arr = FindLatsetArray(path);
                for (i = 0; i < latest_arr; i++)
                {
                    if (!arrPath[i].Contains('['))
                        jobj = jobj[arrPath[i]];
                    else
                    {
                        int index = arrPath[i].Length - 1;
                        while (char.IsDigit(arrPath[i][index - 1]))
                            index--;
                        location = arrPath[i].Substring(0, arrPath[i].IndexOf('['));//arrPath[i].Length - 3);
                        num = arrPath[i].Substring(arrPath[i].IndexOf('[') + 1, arrPath[i].Length - arrPath[i].LastIndexOf(']'));
                        if (arrPath[i].Contains("dataset"))
                        {
                            jobj = jobj[location]["value"][int.Parse(num)];
                        } else
                        {
                            jobj = jobj[location][int.Parse(num)];
                        } 
                        // [int.Parse(arrPath[i][arrPath[i].Length - 2].ToString())];
                        //jobj = jobj[location][int.Parse(arrPath[i][arrPath[i].Length - 2].ToString())];
                    }
                    //location = arrPath[i].Substring(0, arrPath[i].Length - 3);
                    //jobj = jobj[location][int.Parse(arrPath[i][arrPath[i].Length - 2].ToString())];
                }
                if (arrPath.Length == latest_arr || i == latest_arr)
                {  
                    if (arrPath[i].Contains('['))
                    {
                        location = arrPath[i].Substring(0, arrPath[i].IndexOf('[') != -1 ? arrPath[i].IndexOf('[') : arrPath[i].Length);//arrPath[i].Length - 3);
                        num = arrPath[i].Substring(arrPath[i].IndexOf('[') + 1, arrPath[i].Length - arrPath[i].LastIndexOf(']'));
                    }
                }
                
                JArray jarr = new JArray(); 
                if(location.Contains("dataset")){
                    jarr =  jobj[location]["value"] as JArray;
                } else {
                    jarr =  jobj[location] as JArray;
                }
                
                JToken jo = null;
                if(jarr.Count() > 1){
                    jo = jarr[int.Parse(num)];
                } else {
                    return jarr.ToString();
                }
                for(int j = i + 1; j < arrPath.Length; j++){     
                    if(jo.ToObject<JObject>().ContainsKey(arrPath[j])) {
                        if(jo[arrPath[j]].ToString().StartsWith("[")) {
                            jo = jo[arrPath[j]][0];
                        } else {
                            jo = jo[arrPath[j]];
                        }   
                    }                    
                }

                return jo.ToString();
            }
        }
        public static string getValueByPath(JToken valuejs, string path, string subform_end = ""){
            if(path == "") return "";
            bool specific_path = path.Contains(".") && path.Contains("[");
            if(specific_path){
                return GetSpecificValue(valuejs, path);
            }
            string[] c_path = path.Contains(".") ? path.Split('.') : path.Split(','); 
            string colname = c_path[c_path.Length - 1]; 
            for(int pathi = 0; pathi <  c_path.Length; pathi++)
            {
                if (c_path[pathi] != colname && pathi != 0) {
                    c_path[pathi] = c_path[pathi] + subform_end;
                }
            }
            JObject valueObject = valuejs.ToObject<JObject>();
            int len = c_path.Length;
            JToken current = valuejs;
            

            for(int i = 0; i < len; i++){ 
                if(current == null || current.ToString() == "" || current.ToString() == "[]" || current.ToString() == "{}") return "";
                current = current[c_path[i]] is JArray && current[c_path[i]] != null && current[c_path[i]].Count() > 0 ? current[c_path[i]].FirstOrDefault() : current.ToObject<JObject>().ContainsKey(c_path[i]) ? current[c_path[i]] : null;
                
            }
             
            return current == null ?  "" : current.ToString();
            
            
            
            if(len == 1){
                if(valueObject.ContainsKey(colname)){
                    return valuejs[colname].ToString();
                } else {
                    return "";
                }
            } 
            if(len > 2) {
                if(valuejs.ToObject<JObject>().ContainsKey(c_path[c_path.Length - 2] + subform_end)){
                    if(valuejs[c_path[c_path.Length - 2] + subform_end]?.ToString() == "") return "";
                    return valuejs[c_path[c_path.Length - 2] + subform_end] is JArray ? valuejs[c_path[c_path.Length - 2] + subform_end].FirstOrDefault()[colname].ToString() : valuejs[c_path[c_path.Length - 2] + subform_end][colname].ToString();
                }
            } else { 
                if (colname == "") return "";
                if(valueObject.ContainsKey(c_path[len - 2]) && valueObject[c_path[len-2]] != null && valueObject[c_path[len-2]].ToObject<JObject>().ContainsKey(colname)){
                    return valuejs[c_path[len - 2]][colname].ToString();
                } 
                
                if(valueObject.ContainsKey(colname)){
                    return valuejs[colname].ToString();
                }
            }

            return "";
        }

        public static void setValueByPath(JToken valuejs,  JToken new_input, string path){
            if(path == "" || valuejs == null) return; 
            string[] c_path = path.Split('.'); 
            string colname = c_path[c_path.Length - 1]; 
            JObject valueObject = valuejs.ToObject<JObject>();
            int len = c_path.Length;
            JToken current = valuejs;
            int out_new_input = 0;
            new_input = !(new_input.ToString().StartsWith("0")) && int.TryParse(new_input.ToString(), out out_new_input) ? int.Parse(new_input.ToString()) : new_input;
            if(len > 2) {
                if(valueObject.ContainsKey(c_path[c_path.Length - 2])){
                           
                    if(valuejs[c_path[c_path.Length - 2]] == null || valuejs[c_path[c_path.Length - 2]].ToString() == ""){
                        JObject new_jo = new JObject();
                        new_jo[colname] = new_input;
                        valuejs[c_path[c_path.Length - 2]] = new_jo;     
                                 
                        return;
                    }
                    
                    if(valuejs[c_path[c_path.Length - 2]].Count() > 1){
                        for(int k = 0; k < valuejs[c_path[c_path.Length - 2]].Count();k++){
                            valuejs[c_path[c_path.Length - 2]][k][colname] = new_input;
                        }   
                    } else if(valuejs[c_path[c_path.Length - 2]].Count() == 1) {
                        valuejs[c_path[c_path.Length - 2]].FirstOrDefault()[colname] = new_input;
                    } 
                }
            } else { 
                if (colname == "") return;
                
                if(len > 1 && valueObject.ContainsKey(c_path[len - 2])){
                    if(valuejs[c_path[len-2]] == null || valuejs[c_path[len - 2]].ToString() == "") {
                        JObject new_jo = new JObject();
                        new_jo[colname] = new_input;
                        valuejs[c_path[c_path.Length - 2]] = new_jo;     
                        return;
                    }
                    
                    valuejs[c_path[len - 2]][colname] = new_input;
                    return;
                } else if(len > 1 && valueObject.ContainsKey(c_path[len - 2])){
                    
                    JArray arr = valuejs[c_path[len-2]] as JArray;
                    arr.Add(new_input);
                    valuejs[c_path[len - 2]] = arr;
                }     

                
                valuejs[colname] = new_input;
                 
                
            }
        }

    }
}