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
            ILGenerator ilg = interpretBuilder.GetILGenerator();

            // define some labels
            Label while_loop = ilg.DefineLabel();
            Label loop_condition = ilg.DefineLabel();
            Label plus = ilg.DefineLabel();
            Label minus = ilg.DefineLabel();
            Label less_than = ilg.DefineLabel();
            Label greater_than = ilg.DefineLabel();
            Label period = ilg.DefineLabel();
            Label comma = ilg.DefineLabel();
            Label left_bracket = ilg.DefineLabel();
            Label right_bracket = ilg.DefineLabel();
            Label default_char = ilg.DefineLabel();
            Label left_bracket_condition = ilg.DefineLabel();

            //.method public hidebysig static void  Interpret(string script) cil managed
            //{
            //  // Code size       360 (0x168)
            //  .maxstack  3
            //  .locals init ([0] char[] data,
            ilg.DeclareLocal(typeof(char[]));
            //           [1] int32 inst_p,
            ilg.DeclareLocal(typeof(int));
            //           [2] int32 data_p,
            ilg.DeclareLocal(typeof(int));
            //           [3] int32 level,
            ilg.DeclareLocal(typeof(int));
            //           [4] int32 V_4,
            ilg.DeclareLocal(typeof(int));
            //           [5] char CS$0$0000)
            ilg.DeclareLocal(typeof(char));
            //  IL_0000:  ldc.i4     0x7530
            ilg.Emit(OpCodes.Ldc_I4, 0x7530);       // load the number 30000 onto the stack
            //  IL_0005:  newarr     [mscorlib]System.Char
            ilg.Emit(OpCodes.Newarr, typeof(char)); // new char[30000]
            //  IL_000a:  stloc.0
            ilg.Emit(OpCodes.Stloc_0);              // pop value (address of the array) from the stack and assign it to data
            //  IL_000b:  ldc.i4.0
            ilg.Emit(OpCodes.Ldc_I4_0);             // push 0 onto the stack
            //  IL_000c:  stloc.1
            ilg.Emit(OpCodes.Stloc_1);              // pop value from the stack and store as inst_p
            //  IL_000d:  ldc.i4.0
            ilg.Emit(OpCodes.Ldc_I4_0);             // push 0 onto the stack
            //  IL_000e:  stloc.2
            ilg.Emit(OpCodes.Stloc_2);              // pop value from the stack and store as data_p
            //  IL_000f:  br         IL_015b
            ilg.Emit(OpCodes.Br, loop_condition);   // check the loop condition
            ilg.MarkLabel(while_loop);              // if it was true, come back here and start the while loop
            //  IL_0014:  ldarg.0
            ilg.Emit(OpCodes.Ldarg_0);              // load script onto the stack
            //  IL_0015:  ldloc.1
            ilg.Emit(OpCodes.Ldloc_1);              // load inst_p onto stack
            //  IL_0016:  callvirt   instance char [mscorlib]System.String::get_Chars(int32)
            ilg.Emit(OpCodes.Callvirt, typeof(string).GetMethod("get_Chars", new Type[]{ typeof(int) }));  // get the char in script at inst_p
            //  IL_001b:  stloc.s    CS$0$0000
            ilg.Emit(OpCodes.Stloc_S, (byte)5);     // store script[inst_p] in local var 5
            //  IL_001d:  ldloc.s    CS$0$0000
            ilg.Emit(OpCodes.Ldloc_S, (byte)5);     // push script[inst_p] back onto the stack
            //  IL_001f:  ldc.i4.s   43
            ilg.Emit(OpCodes.Ldc_I4_S, (byte)43);   // pushes '+' onto the stack
            //  IL_0021:  sub
            ilg.Emit(OpCodes.Sub);                  // script[inst_p] - '+' onto stack
            //  IL_0022:  switch     ( 
            //                        IL_007a,
            //                        IL_00bb,
            //                        IL_0094,
            //                        IL_00ae)
            ilg.Emit(OpCodes.Switch, new Label[] { plus, comma, minus, period });   // switch on one of these labels
            //  IL_0037:  ldloc.s    CS$0$0000
            ilg.Emit(OpCodes.Ldloc_S, (byte)5);    // if fall through, push script[inst_p] back onto stack
            //  IL_0039:  ldc.i4.s   60
            ilg.Emit(OpCodes.Ldc_I4_S, (byte)60);   // pushes '<' onto the stack
            //  IL_003b:  sub
            ilg.Emit(OpCodes.Sub);                  // script[inst_p] - '<' onto stack
            //  IL_003c:  switch     ( 
            //                        IL_0071,
            //                        IL_0157,
            //                        IL_0068)
            ilg.Emit(OpCodes.Switch, new Label[] { less_than, default_char, greater_than });   // switch on one of these labels
            //  IL_004d:  ldloc.s    CS$0$0000
            ilg.Emit(OpCodes.Ldloc_S, (byte)5);    // if fall through, push script[inst_p] back onto stack
            //  IL_004f:  ldc.i4.s   91
            ilg.Emit(OpCodes.Ldc_I4_S, (byte)91);   // pushes '[' onto the stack
            //  IL_0051:  sub
            ilg.Emit(OpCodes.Sub);                  // script[inst_p] - '[' onto stack
            //  IL_0052:  switch     ( 
            //                        IL_00c9,
            //                        IL_0157,
            //                        IL_0112)
            ilg.Emit(OpCodes.Switch, new Label[] { left_bracket, default_char, right_bracket });   // switch on one of these labels
            //  IL_0063:  br         IL_0157
            ilg.Emit(OpCodes.Br, default_char);     // if no matches, branch to default_char
            ilg.MarkLabel(greater_than);            // begin '>' processing
            //  IL_0068:  ldloc.2
            ilg.Emit(OpCodes.Ldloc_2);              // load data_p onto stack
            //  IL_0069:  ldc.i4.1
            ilg.Emit(OpCodes.Ldc_I4_1);             // push 1 onto stack
            //  IL_006a:  add
            ilg.Emit(OpCodes.Add);                  // increment data_p
            //  IL_006b:  stloc.2
            ilg.Emit(OpCodes.Stloc_2);              // store result in data_p
            //  IL_006c:  br         IL_0157
            ilg.Emit(OpCodes.Br, default_char);     // break
            ilg.MarkLabel(less_than);               // begin '<' processing
            //  IL_0071:  ldloc.2
            ilg.Emit(OpCodes.Ldloc_2);              // load data_p onto stack
            //  IL_0072:  ldc.i4.1
            ilg.Emit(OpCodes.Ldc_I4_1);             // push 1 onto stack
            //  IL_0073:  sub
            ilg.Emit(OpCodes.Sub);                  // decrement data_p
            //  IL_0074:  stloc.2
            ilg.Emit(OpCodes.Stloc_2);              // store result in data_p
            //  IL_0075:  br         IL_0157
            ilg.Emit(OpCodes.Br, default_char);     // break
            ilg.MarkLabel(plus);                    // begin '+' processing
            //  IL_007a:  ldloc.0
            ilg.Emit(OpCodes.Ldloc_0);              // load data array onto stack
            //  IL_007b:  ldloc.2
            ilg.Emit(OpCodes.Ldloc_2);              // load data_p onto stack
            //  IL_007c:  ldelema    [mscorlib]System.Char
            ilg.Emit(OpCodes.Ldelema, typeof(char));// load the address of the char at data[data_p]
            //  IL_0081:  dup
            ilg.Emit(OpCodes.Dup);                  // duplicate the address
            //  IL_0082:  ldobj      [mscorlib]System.Char
            ilg.Emit(OpCodes.Ldobj, typeof(char));  // load the value at data[data_p]
            //  IL_0087:  ldc.i4.1
            ilg.Emit(OpCodes.Ldc_I4_1);             // load 1 onto stack
            //  IL_0088:  add
            ilg.Emit(OpCodes.Add);                  // increment the value from data[data_p]
            //  IL_0089:  conv.u2
            ilg.Emit(OpCodes.Conv_U2);              // convert to a uint16
            //  IL_008a:  stobj      [mscorlib]System.Char
            ilg.Emit(OpCodes.Stobj, typeof(char));  // store at address of data[data_p]
            //  IL_008f:  br         IL_0157
            ilg.Emit(OpCodes.Br, default_char);     // break
            ilg.MarkLabel(minus);                   // begin processing '-'
            //  IL_0094:  ldloc.0
            ilg.Emit(OpCodes.Ldloc_0);              // load data array onto stack
            //  IL_0095:  ldloc.2
            ilg.Emit(OpCodes.Ldloc_2);              // load data_p onto stack
            //  IL_0096:  ldelema    [mscorlib]System.Char
            ilg.Emit(OpCodes.Ldelema, typeof(char));// load the address of the char at data[data_p]
            //  IL_009b:  dup
            ilg.Emit(OpCodes.Dup);                  // duplicate the address
            //  IL_009c:  ldobj      [mscorlib]System.Char
            ilg.Emit(OpCodes.Ldobj, typeof(char));  // load the value at data[data_p]
            //  IL_00a1:  ldc.i4.1
            ilg.Emit(OpCodes.Ldc_I4_1);             // load 1 onto stack
            //  IL_00a2:  sub
            ilg.Emit(OpCodes.Sub);                  // decrement the value from data[data_p]
            //  IL_00a3:  conv.u2
            ilg.Emit(OpCodes.Conv_U2);              // convert to a uint16
            //  IL_00a4:  stobj      [mscorlib]System.Char
            ilg.Emit(OpCodes.Stobj, typeof(char));  // store at address of data[data_p]
            //  IL_00a9:  br         IL_0157
            ilg.Emit(OpCodes.Br, default_char);     // break
            ilg.MarkLabel(period);                  // begin '.' processing
            //  IL_00ae:  ldloc.0
            ilg.Emit(OpCodes.Ldloc_0);              // load data array onto stack
            //  IL_00af:  ldloc.2
            ilg.Emit(OpCodes.Ldloc_2);              // load data_p onto stack
            //  IL_00b0:  ldelem.u2
            ilg.Emit(OpCodes.Ldelem_U2);            // load value at data[data_p]
            //  IL_00b1:  call       void [mscorlib]System.Console::Write(char)
            ilg.Emit(OpCodes.Call, typeof(System.Console).GetMethod("Write", new Type[] { typeof(char) })); // write the char to console
            //  IL_00b6:  br         IL_0157
            ilg.Emit(OpCodes.Br, default_char);     // break
            ilg.MarkLabel(comma);                   // begin ',' processing
            //  IL_00bb:  ldloc.0
            ilg.Emit(OpCodes.Ldloc_0);              // load data array onto stack
            //  IL_00bc:  ldloc.2
            ilg.Emit(OpCodes.Ldloc_2);              // load data_p onto stack
            //  IL_00bd:  call       int32 [mscorlib]System.Console::Read()
            ilg.Emit(OpCodes.Call, typeof(System.Console).GetMethod("Read", Type.EmptyTypes)); // read a char from console
            //  IL_00c2:  conv.u2
            ilg.Emit(OpCodes.Conv_U2);              // convert to uint16
            //  IL_00c3:  stelem.i2
            ilg.Emit(OpCodes.Stelem_I2);            // store read value at data[data_p]
            //  IL_00c4:  br         IL_0157
            ilg.Emit(OpCodes.Br, default_char);     // break
            ilg.MarkLabel(left_bracket);            // begin '[' processing
            //  IL_00c9:  ldloc.0
            ilg.Emit(OpCodes.Ldloc_0);              // load data array onto stack
            //  IL_00ca:  ldloc.2
            ilg.Emit(OpCodes.Ldloc_2);              // load data_p onto stack
            //  IL_00cb:  ldelem.u2
            ilg.Emit(OpCodes.Ldelem_U2);            // load value at data[data_p]
            //  IL_00cc:  brtrue     IL_0157
            ilg.Emit(OpCodes.Brtrue, default_char); // if data[data_p] != 0, break
            //  IL_00d1:  ldc.i4.0
            ilg.Emit(OpCodes.Ldc_I4_0);             // otherwise, load 0
            //  IL_00d2:  stloc.3
            ilg.Emit(OpCodes.Stloc_3);              // level = 0
            //  IL_00d3:  br.s       IL_0103
            ilg.Emit(OpCodes.Br_S, left_bracket_condition); // branch to the condition for the left_bracket_loop
            //  IL_00d5:  ldarg.0
            //  IL_00d6:  ldloc.1
            //  IL_00d7:  callvirt   instance char [mscorlib]System.String::get_Chars(int32)
            //  IL_00dc:  ldc.i4.s   93
            //  IL_00de:  bne.un.s   IL_00e3
            //  IL_00e0:  ldloc.3
            //  IL_00e1:  brfalse.s  IL_0157
            //  IL_00e3:  ldarg.0
            //  IL_00e4:  ldloc.1
            //  IL_00e5:  callvirt   instance char [mscorlib]System.String::get_Chars(int32)
            //  IL_00ea:  ldc.i4.s   93
            //  IL_00ec:  bne.un.s   IL_00f4
            //  IL_00ee:  ldloc.3
            //  IL_00ef:  ldc.i4.1
            //  IL_00f0:  sub
            //  IL_00f1:  stloc.3
            //  IL_00f2:  br.s       IL_0103
            //  IL_00f4:  ldarg.0
            //  IL_00f5:  ldloc.1
            //  IL_00f6:  callvirt   instance char [mscorlib]System.String::get_Chars(int32)
            //  IL_00fb:  ldc.i4.s   91
            //  IL_00fd:  bne.un.s   IL_0103
            //  IL_00ff:  ldloc.3
            //  IL_0100:  ldc.i4.1
            //  IL_0101:  add
            //  IL_0102:  stloc.3
            ilg.MarkLabel(left_bracket_condition);      // start the left bracket condition
            //  IL_0103:  ldloc.1
            ilg.Emit(OpCodes.Ldloc_1);                  // load inst_p
            //  IL_0104:  ldc.i4.1
            ilg.Emit(OpCodes.Ldc_I4_1);                 // load 1
            //  IL_0105:  add
            ilg.Emit(OpCodes.Add);                      // increment inst_p
            //  IL_0106:  dup
            ilg.Emit(OpCodes.Dup);                      // duplicate inst_p
            //  IL_0107:  stloc.1
            ilg.Emit(OpCodes.Stloc_1);                  // store new inst_p
            //  IL_0108:  ldarg.0
            //  IL_0109:  callvirt   instance int32 [mscorlib]System.String::get_Length()
            //  IL_010e:  blt.s      IL_00d5
            //  IL_0110:  br.s       IL_0157
            //  IL_0112:  ldloc.0
            //  IL_0113:  ldloc.2
            //  IL_0114:  ldelem.u2
            //  IL_0115:  brfalse.s  IL_0157
            //  IL_0117:  ldc.i4.0
            //  IL_0118:  stloc.s    V_4
            //  IL_011a:  br.s       IL_014f
            //  IL_011c:  ldarg.0
            //  IL_011d:  ldloc.1
            //  IL_011e:  callvirt   instance char [mscorlib]System.String::get_Chars(int32)
            //  IL_0123:  ldc.i4.s   91
            //  IL_0125:  bne.un.s   IL_012b
            //  IL_0127:  ldloc.s    V_4
            //  IL_0129:  brfalse.s  IL_0157
            //  IL_012b:  ldarg.0
            //  IL_012c:  ldloc.1
            //  IL_012d:  callvirt   instance char [mscorlib]System.String::get_Chars(int32)
            //  IL_0132:  ldc.i4.s   91
            //  IL_0134:  bne.un.s   IL_013e
            //  IL_0136:  ldloc.s    V_4
            //  IL_0138:  ldc.i4.1
            //  IL_0139:  sub
            //  IL_013a:  stloc.s    V_4
            //  IL_013c:  br.s       IL_014f
            //  IL_013e:  ldarg.0
            //  IL_013f:  ldloc.1
            //  IL_0140:  callvirt   instance char [mscorlib]System.String::get_Chars(int32)
            //  IL_0145:  ldc.i4.s   93
            //  IL_0147:  bne.un.s   IL_014f
            //  IL_0149:  ldloc.s    V_4
            //  IL_014b:  ldc.i4.1
            //  IL_014c:  add
            //  IL_014d:  stloc.s    V_4
            //  IL_014f:  ldloc.1
            //  IL_0150:  ldc.i4.1
            //  IL_0151:  sub
            //  IL_0152:  dup
            //  IL_0153:  stloc.1
            //  IL_0154:  ldc.i4.0
            //  IL_0155:  bge.s      IL_011c
            //  IL_0157:  ldloc.1
            //  IL_0158:  ldc.i4.1
            //  IL_0159:  add
            //  IL_015a:  stloc.1
            ilg.MarkLabel(loop_condition);       // check the loop condition
            //  IL_015b:  ldloc.1
            ilg.Emit(OpCodes.Ldloc_1);          // load local variable 1 (inst_p) onto stack
            //  IL_015c:  ldarg.0
            ilg.Emit(OpCodes.Ldarg_0);          // load arg 0 (script) onto stack
            //  IL_015d:  callvirt   instance int32 [mscorlib]System.String::get_Length()
            ilg.Emit(OpCodes.Callvirt, typeof(string).GetMethod("get_Length", Type.EmptyTypes)); // get the length of the string
            //  IL_0162:  blt        IL_0014
            ilg.Emit(OpCodes.Blt, while_loop);  // if inst_p < length of the string, go to while_loop
            //  IL_0167:  ret
            ilg.Emit(OpCodes.Ret);              // return
            //} // end of method Program::Interpret



            // build the main method (calls Interpret(script))
            MethodBuilder mainBuilder = typeBuilder.DefineMethod("Main", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig, typeof(int), new Type[] { typeof(string[]) });
            ILGenerator ilg_main = mainBuilder.GetILGenerator();
            ilg_main.Emit(OpCodes.Ldstr, script); // push the script onto the stack
            ilg_main.Emit(OpCodes.Call, interpretBuilder); // call Interpret(script)
            ilg_main.Emit(OpCodes.Ldc_I4_0); // push 0 onto the stack
            ilg_main.Emit(OpCodes.Ret);  // return 0

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
