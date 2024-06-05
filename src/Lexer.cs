
namespace FormulaEvaluator {
    public enum Token {
        Identifier,
        Number,
        Var,
        Equal,
        Plus,
        Minus,
        Mul,
        Div,
        Mod,
        String,
        Path,
        LParen,
        RParen,
        EOF,
        ILLEGAL
    }

    public static class Lexer {
        public static IEnumerable<(Token, object? value)> ParseTokens(string input){
            var pos = 0;
            while(pos < input.Length){
                
                var c = input[pos];
                if(char.IsWhiteSpace(c)){
                    pos++;
                    continue;
                }

                if(char.IsLetter(c)){
                    var start = pos;
                    while(pos < input.Length && (char.IsLetterOrDigit(input[pos]) || input[pos] == '_')){
                        pos++;
                    }
                    var value = input.Substring(start, pos - start);
                    var token = value.Contains("var_") ? Token.Var : Token.Identifier;
                    yield return (token, value);
                    continue;
                }

                if(char.IsDigit(c)){
                    var start = pos;
                    while(pos < input.Length && (char.IsLetterOrDigit(input[pos]) || input[pos] == '_')){
                        pos++;
                    }
                    var value = input.Substring(start, pos - start);
                    yield return (Token.Number, int.Parse(value));
                    continue;
                }

                if(c == '\"'){
                    var start = pos + 1;
                    pos++;
                    while(pos < input.Length && input[pos] != '\"'){
                        pos++;
                    }

                    if(pos >= input.Length){
                        throw new Exception("Unclosed String Literal");
                    }

                    var value = input.Substring(start, pos - start);
                    pos++;
                    yield return (Token.String, value);
                    pos++;
                    continue;
                }

                if(c == '<' && input[pos + 1] == '<'){
                    var start = pos + 2;
                    pos += 2;
                    
                    while(pos < input.Length && (input[pos] != '>')){
                        pos++;
                    }

                    if(pos >= input.Length){
                        throw new Exception("Undefined path");
                    }

                    var value = input.Substring(start, pos - start);
                    pos += 2;
                    yield return (Token.Path, value);
                    pos++;
                    continue;
                }

                yield return c switch {
                    '=' => (Token.Equal, '='),
                    '+' => (Token.Plus, '+'),
                    '-' => (Token.Minus, '-'),
                    '*' => (Token.Mul, '*'),
                    '%' => (Token.Mod, '%'),
                    '/' => (Token.Div, '/'),
                    '(' => (Token.LParen, '('),
                    ')' => (Token.RParen, ')'),
                    _ => (Token.ILLEGAL , c)
                };

                pos++;
            }

            yield return (Token.EOF, null);
        } 
    }
}