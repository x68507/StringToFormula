# StringToFormula
Parses a C# string representing an expression, an equality/inequality, or a Boolean term and returns a numeric value.  

## How to Use
The entire parser is in a single class so you can add just 1 file to your project and you're able to parse any string input and return a double value.  No need to include an external DLL if you're trying to create  small executible without external dependecies.

```c#
string input = "1 + 2 / 3 - 5 * 2.5";

var parser = new StringToFormula();
double output = parser.Eval(input);    // expected -10.8333
```

## Coverage
This parser will handle:

* Simple mathmetical expressions including PEMDAS (paranthesis, exponents, multiplication, division, addition, and subtraction)
  * The order of evaluation is always PEMDAS and then left-to-right
* Equalities including:
  * ```==``` : equals
  * ```!=``` : not equals
  * ```<=``` : less than or equal to
  * ```>=``` : greater than or equal to
  * ```<```  : less than
  * ```>```  : greater than
* Boolean AND and OR operators
  * ```&```: default AND (customizable)
  * ```|```: default OR (customizable)
* Negation operator ```!``` will convert any 0 to 1 and any non-zero to 0

## Sample Passing Code
### Expressions
* ```(1) + (2)``` = 3
* ```(1--1)^1.52``` = 2.86791
* ```(2+-2^0.2)/10*5+(1--1)^1.52``` =  3.293561
* ```(2+-2^0.2)/10*5+(1--1)^1.52*-2``` =  -5.310
* ```(-1 * -4)``` =  4 
* ```1/2+3``` =  3.5 
* ```-1 --1``` =  0 
* ```+1 --1``` =  2 
* ```(-1 * 4)``` =  -4 
* ```-1 * 4``` =  -4 
* ```bad input``` =  double.NaN 
* ```1/0``` =  double.PositiveInfinity 
* ```-1/0``` =  double.NegativeInfinity 
* ```1 * -4``` =  -4 
* ```-1 * -4``` =  4 
* ```2^3``` =  8 
* ```-2^3``` =  -8 
* ```2^-2``` =  0.25 
* ```-2^2``` =  4 
* ```-4 / -2``` =  2 
* ```-4 / 2``` =  -2 
* ```4 / -2``` =  -2 
* ```2+-2``` =  0 
* ```(2+-2)``` =  0 
* ```(12 / 12 * (10 + 10)) / 2``` =  10 
* ```(12 / 12.5 * (10 + 10)) / 2.1``` =  9.142857
* ```(2 + -2) / 10 * 5 + (1 - -1) * -2``` =  -4 
* ```(2+-2)/10*5+(1--1)^12*-2``` =  -8192 
## Equalities
* ```1 == 1``` =  1
* ```1 == 2``` =  0
* ```1 < 2``` =  1
* ```1<=1``` =  1
* ```1 != 2``` =  1
* ```1 > 2``` =  0
* ```1>=1``` =  1
* ```2 != 1``` =  1
## Boolean
* ```1 * 0 & 1``` = 0
* ```1 & 0``` = 0
* ```1 & 1``` = 1
* ```0 & 0``` = 1
* ```1 & 0 & 1 & 0``` = 0
* ```1 & 0 & 0 | 0``` = 0
* ```1 & 0 & 0 | 1``` = 1
* ```1 & 0 & 0 | 0 | 1``` = 1
* ```2 & 0 | 2``` = 1
* ```1 | 1 & 0 | 0``` = 1
* ```(1 | 0) & (0 | 1)``` = 1
* ```(1 | 0) | (0 & 1)``` = 1
* ```(1 | 0) & (0 & 0)``` = 1
* ```(1 | 0) & (1 & 0)``` = 0
* ```(1 | 0) | (1 & 0)``` = 1
* ```1 + 2 & 3 + 4``` = 1
* ```0 / 1 | 1 / 1``` = 1
## Negation
* ```!1 + 2``` = 2
* ```!0 + !0 + !0``` = 3
* ```!1``` = 0
* ```!0``` = 1
* ```!(1)``` = 0
* ```!(!(0 / 1))``` = 0
* ```!!1``` = 0
* ```!!0``` = 1

## Custom Settings
You can customize the boolean ```AND``` and ```OR``` operators.  By default, these are ```&``` and ```|``` respectively, but some languages use ***&&*** and ***||*** as the operators.  To change these default values, simply access the ```GetSettings()``` method and update the operator:

```c#
string input = "(1 || 0) && (1)";

var parser = new StringToFormula();

var ps = parser.GetSettings();
ps.AndString = "&&";
ps.OrString = "||";

double output = parser.Eval(input);   // expected 1
```
