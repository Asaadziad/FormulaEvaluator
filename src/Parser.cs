using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;


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
            int IVal;
            double Val;
            string SVal;
            public Expr(){
                SVal = null;
            }
            
            public Expr(int val){
                IVal = val;
            }
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
            virtual public string Type() { return ""; }
            virtual public object Eval(JToken source,JObject storage) {
                if(SVal != null && storage.ContainsKey(SVal)) { 
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
            public override string Type()
            {
                return "PATH";
            }
            public override object Eval(JToken source, JObject storage)
            {
                string type = ValExpr != null ? ValExpr.Type() : "";
                object vExpr = ValExpr != null ? ValExpr.Eval(source, storage) : null; 
                string[] paths = Path?.ToString().Split('.');
                 
                if(ValExpr == null){
                    //Console.WriteLine(paths[0]);
                    if(paths[0].Contains("query_") || paths[0].Contains("var_") || paths[0].Contains("dataset_")) {
                        Console.WriteLine("i get it from storage");

                        return Utility.getValueByPath(storage, Path.ToString()); 
                    } else {
                        if(Path.Contains("DYNAMIC")){
                           
                            return Dynamic.GetDynamicValue(Path);
                        }
                        return Utility.getValueByPath(source, Path.ToString());
                    }
                } else {
                    JToken input = type == "String" ? vExpr.ToString() : double.Parse(vExpr != null && vExpr.ToString() != "" ? vExpr.ToString() : "0");
                    Console.WriteLine("input: " + input + " type: " + type);
                    if(paths[0].Contains("query_") || paths[0].Contains("var_") || paths[0].Contains("dataset_")){          
                        Utility.setValueByPath(storage, input, Path);
                    } else {
                        Utility.setValueByPath(source, input, Path);
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
            public override string Type()
            {
                return "Number";
            }
            public override object Eval(JToken source,JObject storage) {  
                Console.WriteLine("Eval Number this was called");
                return Val;
            }
        }
        public class StringExpr: Expr {
            string Val; 
            public StringExpr(string val){
                Val = val;
            }

            public override string Type()
            {
                return "String";
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
                bool is_string_type = lhs.Type() == "String";
                double outVal;
                if(l_object == null || r_object == null) return null;
                Console.WriteLine(l_object.ToString() + " is a String: " + is_string_type);
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
                    Console.WriteLine("Parsed number : " + this.Tokens[this.pos].v.ToString());
                     
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
        
    }
}