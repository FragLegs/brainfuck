using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace brainfuck
{
    class Program
    {
        /// <summary>
        /// Main entry point for Brainfuck interpreter/compiler
        /// </summary>
        /// <param name="args">[-c] (input script|input filename)</param>
        static void Main(string[] args)
        {
            bool compile = false;
            string script = "";
            string outfile = "";

            // check for correctness of args
            if (args.Length == 0)
            {
                Usage();
                return;
            }
            
            // check for compile flag
            if (args[0] == "-c")
            {
                compile = true;

                // make sure there is an output filename
                if (args.Length < 2)
                {
                    Usage();
                    return;
                }

                outfile = args[1];
                
                // continue as usual, ignoring the first two arguments
                args = args.Skip(2).ToArray();
                                
                // make sure there are more args
                if (args.Length == 0)
                {
                    Usage();
                    return;
                }
            }

            // if first arg is a valid filename
            if (System.IO.File.Exists(args[0]))
            {
                // read script from the file
                script = System.IO.File.ReadAllText(args[0]);
            }
            else
            {
                // concat all args (allows us to input arbitrary scripts)
                script = String.Join(" ", args);
            }

            if (compile) Compile(script, outfile);
            else Interpret(script); // otherwise run the interpreter
        }


        /// <summary>
        /// Run the brainfuck interpreter
        /// </summary>
        /// <param name="script">The brainfuck script to execute</param>
        public static void Interpret(string script)
        {
            // the data array
            char[] data = new char[30000];

            // the index into the script
            int inst_p = 0;

            // the data pointer
            int data_p = 0;

            // process the script
            while (inst_p < script.Length)
            {
                switch (script[inst_p])
                {
                    case '>':               // increment the data pointer
                        data_p++;
                        break;
                    case '<':               // decrement the data pointer
                        data_p--;
                        break;
                    case '+':               // increment the data at the data pointer
                        data[data_p]++;
                        break;
                    case '-':               // decrement the data at the data pointer
                        data[data_p]--;
                        break;
                    case '.':               // print the data at the data pointer
                        Console.Write(data[data_p]);
                        break;
                    case ',':               // read a char and save it to data at the data pointer
                        data[data_p] = (char)Console.Read();
                        break;
                    case '[':               // skip forward to instruction after matching ]
                        if (data[data_p] == 0)  // only if data at data pointer is 0
                        {
                            // keep track of nested []
                            int level = 0;

                            // walk until we find the closing ]
                            while (++inst_p < script.Length)
                            {
                                if (script[inst_p] == ']' && level == 0) break;
                                else if (script[inst_p] == ']') level--;
                                else if (script[inst_p] == '[') level++;
                            }
                        }
                        break;
                    case ']':               // skip backwards to instruction after matching [
                        if (data[data_p] != 0)  // only if data at data pointer is not 0
                        {
                            // keep track of nested []
                            int level = 0;

                            // walk until we find the closing [
                            while (--inst_p >= 0)
                            {
                                if (script[inst_p] == '[' && level == 0) break;
                                else if (script[inst_p] == '[') level--;
                                else if (script[inst_p] == ']') level++;
                            }
                        }
                        break;
                    default:                // pass over any other character
                        break;
                }

                // go to next instruction
                inst_p++;
            }
        }

        /// <summary>
        /// Compile the brainfuck program to an executable
        /// </summary>
        /// <param name="script">The brainfuck script to compile</param>
        /// <param name="outfile">The name of the file to save the executable to</param>
        private static void Compile(string script, string outfile)
        {
            // If outfile ends in "exe", strip it
            if (outfile.EndsWith(".exe")) outfile = outfile.Substring(0, outfile.Length - 4);

            // create the dynamic assembly
            AssemblyName aName = new AssemblyName(outfile);
            AssemblyBuilder asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave);
            
            // create the module
            ModuleBuilder modBuilder = asmBuilder.DefineDynamicModule(outfile, outfile + ".exe", false);

            // create a new type that extends BF
            TypeBuilder typeBuilder = modBuilder.DefineType(outfile + "Class", TypeAttributes.Public | TypeAttributes.Class, typeof(object));

            // build the Interpret function
            MethodBuilder interpretBuilder = typeBuilder.DefineMethod("Interpret", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, typeof(void), new Type[] { typeof(string) });
            MethodInfo mi = typeof(Program).GetMethod("Interpret");

            // TODO: Opcode for the interpret function

            // build the main method (calls Interpret(script))
            MethodBuilder mainBuilder = typeBuilder.DefineMethod("Main", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig, typeof(int), new Type[] { typeof(string[]) });
            ILGenerator ilg = mainBuilder.GetILGenerator();
            ilg.Emit(OpCodes.Ldstr, script); // push the script onto the stack
            ilg.Emit(OpCodes.Call, interpretBuilder); // call Interpret(script)
            ilg.Emit(OpCodes.Ldc_I4_0); // push 0 onto the stack
            ilg.Emit(OpCodes.Ret);  // return 0

            // actually create the type
            typeBuilder.CreateType();

            // Set the entrypoint (thereby declaring it an EXE)
            asmBuilder.SetEntryPoint(mainBuilder, PEFileKinds.ConsoleApplication);
            
            // save the assembly
            asmBuilder.Save(outfile + ".exe");
        }

        /// <summary>
        /// Print usage hints
        /// </summary>
        private static void Usage()
        {
            Console.WriteLine("brainfuck [-c outfile] (script|filename)");
            Console.WriteLine("\t-c will compile the brainfuck program as an executable");
        }
    }
}
