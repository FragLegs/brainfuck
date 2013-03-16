Overview
--------
A brainfuck interpreter & compiler, written in C#.

Usage
-----

brainfuck \[\-c outfile\] \(script | filename\)  
&nbsp;&nbsp;&nbsp;\-c will compile the brainfuck program as an executable

Brainfuck Language
------------------
Brainfuck is a simple programming language.  From Wikipedia \(http://en.wikipedia.org/wiki/Brainfuck\):

The language consists of eight commands, listed below. A brainfuck program is a sequence of these commands, possibly interspersed with other characters (which are ignored). The commands are executed sequentially, except as noted below; an instruction pointer begins at the first command, and each command it points to is executed, after which it normally moves forward to the next command. The program terminates when the instruction pointer moves past the last command.

The brainfuck language uses a simple machine model consisting of the program and instruction pointer, as well as an array of at least 30,000 byte cells initialized to zero; a movable data pointer (initialized to point to the leftmost byte of the array); and two streams of bytes for input and output (most often connected to a keyboard and a monitor respectively, and using the ASCII character encoding).

### Commands ###

The eight language commands, each consisting of a single character:

<table>

<tr><td>Character</td><td>Meaning</td></tr>
<tr><td>></td><td>increment the data pointer (to point to the next cell to the right).</td></tr>
<tr><td><</td><td>decrement the data pointer (to point to the next cell to the left).</td></tr>
<tr><td>\+</td><td>increment (increase by one) the byte at the data pointer.</td></tr>
<tr><td>\-</td><td>decrement (decrease by one) the byte at the data pointer.</td></tr>
<tr><td>\.</td><td>output the byte at the data pointer.</td></tr>
<tr><td>,</td><td>accept one byte of input, storing its value in the byte at the data pointer.</td></tr>
<tr><td>\[</td><td>if the byte at the data pointer is zero, then instead of moving the instruction pointer forward to the next command, jump it forward to the command after the matching ] command.</td></tr>
<tr><td>\]</td><td>if the byte at the data pointer is nonzero, then instead of moving the instruction pointer forward to the next command, jump it back to the command after the matching [ command.</td></tr>

</table>