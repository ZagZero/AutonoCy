using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AutonoCy
{
    class AutonoCy_Main
    {
        private static Interpreter interpreter;
        static bool hadError = false;
        static bool hadRuntimeError = false;

        static void Main(string[] args)
        {
            interpreter = new Interpreter();
            if (args.Length > 1)
            {
                Console.WriteLine("Usage: autonocy [script]");
                System.Environment.Exit(64);
            }
            else if (args.Length == 1)
            {
                runFile(args[0]);
            }
            else
            {
                runPrompt();
            }
        }

        static void runFile(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            run(Encoding.Default.GetString(bytes));

            // Indicate Error
            if (hadError)
            {
                System.Environment.Exit(65);
            }
            if (hadRuntimeError) System.Environment.Exit(70);
        }
        
        static void runPrompt()
        { 
            TextReader input = Console.In;
            
            while (true)
            {
                Console.Write("> ");
                run(input.ReadLine());
                hadError = false;
            }
        }

        static void run(string source)
        {
            Scanner scanner = new Scanner(source);
            List<Token> tokens = scanner.scanTokens();

            /*foreach (Token token in tokens)
            {
                Console.WriteLine(token.toString());
            }*/

            Parser parser = new Parser(tokens);
            List<Stmt> statements = parser.parse();

            if (hadError) return;

            //Console.WriteLine(new AstPrinter().print(expression));
            interpreter.interpret(statements);
        }

        public static void error(int line, string message)
        {
            report(line, "", message);
        }

        public static void runtimeError(RuntimeError e)
        {
            Console.Error.WriteLine(e.Message + "\n[line " + e.token.line + "]");
            hadRuntimeError = true;
        }

        public static void error(Token token, string message)
        {
            if (token.type == TokenTypes.EOF)
            {
                report(token.line, " at end", message);
            }
            else
            {
                report(token.line, " at '" + token.lexeme + "'", message);
            }
        }

        private static void report(int line, string where, string message)
        {
            Console.Error.WriteLine("[line " + line + "] Error" + where + ": " + message);
            hadError = true;
        }


    }
}
