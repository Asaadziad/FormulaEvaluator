
namespace FormulaEvaluator {
    public enum Tokens{
        Identifier,
        Number,
        Var,
        Equal,
        Plus,
        Minus,
        Mul,
        Div,
        String,
        LParen,
        RParen,
        EOF,
        ILLEGAL
    }

    public static class Lexer {
        public static IEnumerable<(Tokens, object? value)> ParseTokens(string input){
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
                    var token = value switch {
                        "var-" => Tokens.Var,
                        _ => Tokens.Identifier 
                    };
                    yield return (token, value);
                    continue;
                }

                if(char.IsDigit(c)){
                    var start = pos;
                    while(pos < input.Length && (char.IsLetterOrDigit(input[pos]) || input[pos] == '_')){
                        pos++;
                    }
                    var value = input.Substring(start, pos - start);
                    yield return (Tokens.Number, int.Parse(value));
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
                    yield return (Tokens.String, value);
                    pos++;
                    continue;
                }

                yield return c switch {
                    '=' => (Tokens.Equal, '='),
                    '+' => (Tokens.Plus, '+'),
                    '-' => (Tokens.Minus, '-'),
                    '*' => (Tokens.Mul, '*'),
                    '/' => (Tokens.Div, '/'),
                    '(' => (Tokens.LParen, '('),
                    ')' => (Tokens.RParen, ')'),
                    _ => (Tokens.ILLEGAL , c)
                };

                pos++;
            }

            yield return (Tokens.EOF, null);
        } 
    }
}