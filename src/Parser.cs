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

            virtual public object? Eval(JObject storage) {
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

            public override object? Eval(JObject storage)
            {
               object? l_object = lhs.Eval(storage);
               object? r_object = rhs.Eval(storage);
               bool is_string_type = false;
               if(l_object?.GetType() == typeof(String) && !storage.ContainsKey(l_object?.ToString())){       
                  is_string_type = true;
               } 
              
                switch (op) { 
                    case Token.Div:   return is_string_type ? l_object : double.Parse(l_object.ToString()) / double.Parse(r_object.ToString());
                    case Token.Mul:   return is_string_type ? l_object : double.Parse(l_object.ToString()) * double.Parse(r_object.ToString());
                    case Token.Plus:  return is_string_type ? l_object?.ToString() + r_object?.ToString() : double.Parse(l_object.ToString()) + double.Parse(r_object.ToString());
                    case Token.Minus: return is_string_type ? l_object : double.Parse(l_object.ToString()) - double.Parse(r_object.ToString());
                    case Token.Equal: {
                        Console.WriteLine("R_OBJECT: " + r_object.ToString());
                        storage[l_object?.ToString()] = r_object?.ToString(); 
                        return storage[l_object?.ToString()]; 
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
            Expr var_name_lhs = new Expr(this.Tokens[this.pos].v?.ToString());
            
            Consume(Token.Var);
            if(this.Tokens[this.pos].Item1 == Token.Equal) {
                Consume(Token.Equal);
                Expr rhs = ParseExpr();
                
                return new BinaryExpr(var_name_lhs, Token.Equal, rhs);
            }
           return var_name_lhs; 
        }
        public Expr ParsePrimary(){
            var current_token = this.Tokens[this.pos].Item1;
           
            switch (current_token){
                case Token.Number:{
                    Expr e = new Expr(double.Parse(this.Tokens[this.pos].v.ToString()));
                    Consume(Token.Number);
                    return e;
                }
                case Token.String: {
                    return null;
                }
                case Token.Var: {
                    return ParseVar();
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
                    Expr rhs = ParsePrimary();
                    return new BinaryExpr(lhs, current_token, rhs);
                }
                
                case Token.Mul:{
                    Consume(current_token);
                    Expr rhs = ParsePrimary();
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
                    Consume(current_token);
                    Expr rhs = ParseBinaryMul();
                    return new BinaryExpr(lhs, current_token, rhs);
                }
                
                case Token.Minus:{
                    Consume(current_token);
                    Expr rhs = ParseBinaryMul();
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

        public void Parse(JObject storage){
            while(this.pos < this.Tokens.Count()){ 
                Expr root = ParseExpr();
                Console.WriteLine("Eval : {0}",root?.Eval(storage)?.ToString()); 
                //Console.WriteLine(storage);
                pos++;
            }
        }   
    }
}