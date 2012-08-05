# bnf from http://hl2glsl.codeplex.com/wikipage?title=HLSL%20BNF
# with a couple of tweaks/fixes

from pyparsing import alphas, alphanums, Word, Literal, Optional, Combine, OneOrMore, ZeroOrMore, Forward, \
		Literal, Keyword, delimitedList, Group, Suppress, oneOf, nestedExpr

import sys
from collections import defaultdict

scope_level = 0

cur_scope = defaultdict(dict)
replace_pass = False

def enterFunc(str, loc, tok):
	global scope_level
	scope_level += 1

def leaveFunc(str, loc, tok):
	global scope_level
	scope_level += 1

def idAction(str, loc, tok):
	# check if the id should be replaced
	name = tok[0]
	if replace_pass:
		if name in cur_scope[scope_level]:
#			print ">> id :", name, " -> ", cur_scope[scope_level][name]
			return cur_scope[scope_level][name]
		else:
			pass
#			print "** id :", name
		

def declVariable(str, loc, tok):
	if not replace_pass:
		name = tok[1]
		cur_scope[scope_level][name] = name
	
def declStruct(str, loc, tok):
	if not replace_pass:
		name = tok[1]
		cur_scope[scope_level][name] = name

def declFunc(str, loc, tok):
	if not replace_pass:
		name = tok[1]
		cur_scope[scope_level][name] = name

def generate_replacement_names():
	curid = "a"
	for i in range(scope_level):
		for (k,v) in cur_scope[i].items():
			cur_scope[i][k] = curid
			o = ord(curid[-1])
			if o < ord('z'):
				curid = curid[:len(curid)-1] + chr(o+1)
			else:
				curid = "a" * (len(curid) + 1)

	#print cur_scope
	
C = Combine
O = Optional
W = Word
Z = ZeroOrMore
X = OneOrMore
L = Literal
K = Keyword
G = Group
S = Suppress
S = lambda x : x
G = lambda x : x

# define my own nestedExpr that doesn't throw away tokens
def nestedExpr2(tok_open, tok_close, expr):
	return L(tok_open) + Z(expr) + L(tok_close)

def delimitedList2(expr, delimiter=','):
	return expr + Z(delimiter + expr)
	
nested = nestedExpr2
delimited = delimitedList2
	
id = W(alphas + "_", alphanums + "_")
digit = W('0123456789', exact=1)
sign = W('+-', exact=1)
E = W('eE', exact=1)
number = \
	C(O(sign) + X(digit)) ^ \
	C(O(sign) + Z(digit) + '.' + X(digit) + O(E + X(digit)) + O('f'))
integer = O(sign) + X(digit)
n1to4 = W("1234", exact=1)
n2to4 = W("234", exact=1)
rgba = W("rgba")
xyzw = W("xyzw")

index = nested('[', ']', integer | id)

storage_class = \
	K('extern') ^ K('static') ^ K('nointerpolation') ^\
	K('shared') ^ K('uniform') ^ K('volatile')
	
type_modifier = K('const') ^ K('row_major') ^ K('column_major')

input_sem_params = \
	K('BINORMAL') ^ K('BLENDINDICES') ^ K('BLENDWEIGHT') ^ K('NORMAL') ^\
	K('POSITION') ^  K('TANGENT') ^ K('VFACE') ^ K('VPOS')

output_sem_params = K('FOG') ^ K('TESSFACTOR') ^ K('DEPTH')

in_out_sem_params = \
	C('POSITION' + O(digit)) ^ \
	C('TEXCOORD' + O(digit)) ^ \
	C('TEXUNIT' + O(digit)) ^ \
	C('COLOR' + O(digit)) ^ \
	'PSIZE'
	
semantic_matrices = \
	C('WORLD' + O('I') + O('T')) ^ \
	C('VIEW' + O('I') + O('T')) ^ \
	C('PROJ' + O('I') + O('T')) ^ \
	C('WORLDVIEW' + O('I') + O('T')) ^ \
	C('WORLDPROJ' + O('I') + O('T')) ^ \
	C('VIEWPROJ' + O('I') + O('T')) ^ \
	C('VIEWPROJECTION' + O('I') + O('T')) ^ \
	C('WORLDVIEWPROJ' + O('I') + O('T')) ^ \
	C('WORLDMATRIXARRAY' + O('I') + O('T'))

semantic_extra = \
	K('MATERIALAMBIENT') ^ K('MATERIALDIFFUSE')

semantical_parameters = \
	input_sem_params ^ \
	output_sem_params ^ \
	in_out_sem_params ^ \
	semantic_matrices ^ \
	semantic_extra

basic_type = L('float') ^ 'int' ^ 'half' ^ 'double' ^ 'bool'

ftype = \
	(C(basic_type + (O(n2to4) + (O('x' + n2to4))))) ^ \
	K("vector") ^ \
	K("matrix") ^ \
	K("sampler") ^ K("sampler1D") ^ K("sampler2D") ^ K("sampler3D") ^ K("samplerCUBE") ^ K("sampler_state") ^ \
	K("texture") ^ K("Texture") ^ K("texture1D") ^ K("texture2D") ^ K("texture3D") ^ K("textureCUBE") ^ \
	K("void") ^ \
	id

in_out_inout = K('in') ^ K('out') ^ K('inout')

assignment_operator = \
	L('=') | '+=' | '-=' | '*=' | '/=' | '%=' | '<<=' | '>>=' | '&=' | '|=' | '^='

param = G(O(in_out_inout) + ftype + id.copy().setParseAction(idAction) + O(':' + semantical_parameters))
params = delimited(param)

term_tail = Forward()
factor = Forward()
term = factor + O(term_tail)
term_tail << (\
	assignment_operator + term |\
	'*' + term |\
	'/' + term |\
	'%' + term)

expression = Forward()
expression_tail = \
	'+' + expression |\
	'-' + expression

expression << (term + O(expression_tail))

prefix_postfix_operators = L('++') | '--'
optional_suffix = O(S('.') + (xyzw | rgba | id))
id_composed = G(id + optional_suffix + O(index))

atom = Forward()
func_call = id + nested('(', ')', delimited(expression))
ctor_call = func_call + optional_suffix

initializers = expression

variable_decl_atom = \
	O(storage_class) + O(type_modifier) + ftype + \
		delimited(id + O(index) + O(':' + semantical_parameters) + O(assignment_operator + initializers))

variable_decl_stmt = variable_decl_atom + S(';')
variable_decl = variable_decl_atom + S(';')
		
atom << (\
	number ^ \
	O(prefix_postfix_operators) + id_composed + O(prefix_postfix_operators) ^ \
	func_call ^ \
	ctor_call ^ \
	nested('{', '}', delimited(expression)))

factor << (\
	O(nested('(', ')', ftype)) + atom | \
	nested('(', ')', expression) | \
	nested('<', '>', id))

return_statement = K('return') + expression + S(';')

variable_assignment = id_composed + assignment_operator + initializers + S(';')

flow_control_words = (K('stop') | K('continue') | K('break') | K('discard')) + S(';')

loop_attributes = \
	K('unroll') + nested('(', ')', integer) | \
	K('loop')

while_attributes = loop_attributes
for_attributes = loop_attributes

function_body = Forward()
statement = Forward()
statement_scope = \
	'{' + Z(statement) + '}' | \
	statement
	
comparison_operators = L('<=') | '>=' | '<' | '>' | '==' | '!='

condition = expression + comparison_operators + expression
	
while_statement = O(while_attributes) + K('while') + nested('(', ')', condition) + statement_scope

do_statement = K('do') + statement_scope + K('while') + nested('(', ')', condition) + S(';')
	
if_attributes = K('flatten') | K('branch')
if_statement = \
	O(if_attributes) + K('if') + nested('(', ')', condition) + \
		statement_scope + \
	O(K('else') + \
		statement_scope)
	
for_statement = \
	O(for_attributes) + K('for') + '(' + \
		O(variable_assignment | variable_decl_atom) + ';' + \
		O(condition) + ';' + \
		O(expression) + ')' + \
			statement_scope
		
switch_attributes = K('call') | K('forcecase') | K('branch') | K('flatten')
switch_statement = \
	switch_attributes + K('switch') + nested('(', ')', expression) + '{' + \
		X((K('case') + integer ^ K('default')) + ':' + O(statement_scope)) + \
	'}'

statement <<( \
	return_statement ^ \
	variable_assignment ^ \
	variable_decl_stmt.setParseAction(declVariable) ^ \
	flow_control_words ^ \
	while_statement ^ \
	do_statement ^ \
	if_statement ^ \
	for_statement ^ \
	switch_statement)

function_body << Z(statement)

sampler_type = \
	K("sampler1D") | K("sampler2D") | K("sampler3D") | K("samplerCUBE") | K("sampler")

sampler_decl = sampler_type + id + S('=') + ftype + S('{') + \
	Z(variable_assignment) + \
	S('}' + ';')

	
function_decl = \
	O(storage_class) + O(type_modifier) + \
	ftype + id + nested('(', ')', params) + O(S(':') + semantical_parameters) + \
		S(L('{').setParseAction(enterFunc) + \
			function_body + \
		L('}').setParseAction(leaveFunc))
		
technique_decl = \
	S(K('technique')) + id + S('{') + \
		Z( \
			G(S(K('pass')) + id + S('{') + \
				Z(\
					id + S('=') + O(K('compile') + id) + (id + '()' ^ expression) + S(';') \
				)) +\
			S('}') \
		) +\
	S('}')

struct_var = G(ftype + id + S(':') + semantical_parameters) + S(';')
struct_decl = \
	S(K('struct')) + id + S('{') + \
		X(struct_var) + \
	S('}' + ';')
	

fx_file = []

debugPrint = False

class fxFunction():
	def __init__(self, tok):
		self.tokens = list(tok)
		
class fxVariable():
	def __init__(self, tok):
		self.tokens = list(tok)
		
class fxStruct():
	def __init__(self, tok):
		self.tokens = list(tok)
		
class fxTechnique():
	def __init__(self, tok):
		self.tokens = list(tok)
		
class fxSampler():
	def __init__(self, tok):
		self.tokens = list(tok)

def dbgPrint(id, tok):
	if debugPrint:
		print(len(fx_file))
		print ">> " + id + ": ", tok

def funcAction(str, loc, tok):
	dbgPrint('func', tok)
	if replace_pass:
		fx_file.append(list(tok))

def varAction(str, loc, tok):
	dbgPrint('var', tok)
	if replace_pass:
		fx_file.append(list(tok))

def structAction(str, loc, tok):
	dbgPrint('struct', tok)
	if replace_pass:
		fx_file.append(list(tok))

def techniqueAction(str, loc, tok):
	dbgPrint('tech', tok)
	if replace_pass:
		fx_file.append(list(tok))

def samplerAction(str, loc, tok):
	dbgPrint('sampler', tok)
	if replace_pass:
		fx_file.append(list(tok))

effect_file = Z(
	function_decl.setParseAction(funcAction) | \
	struct_decl.setParseAction(structAction).addParseAction(declStruct) | \
	variable_decl.setParseAction(varAction).addParseAction(declVariable) | \
	sampler_decl.setParseAction(samplerAction) | \
	technique_decl.setParseAction(techniqueAction))

if len(sys.argv) < 2:
	exit(1)

e = open(sys.argv[1]).readlines()
ee = ""
in_comment = False
for x in e:

	# handle multiline comments
	if in_comment:
		cmt = x.find("*/")
		if cmt != -1:
			x = x[cmt+2:]
			in_comment = False
	else:
		cmt = x.find("/*")
		if cmt != -1:
			x = x[:cmt]
			in_comment = True
			
	if not in_comment:
		# handle single line comments
		cmt = x.find("//")
		if cmt != -1:
			x = x[:cmt]
		
		# skip lines that only consist of whitespace
		if len(x.strip()) > 0:
		
			# insert whitespace to help the tokenizer
			for t in [',', '(', '{', '[']:
				x = x.replace(t, t + ' ')

			for t in [')', '}', ']']:
				x = x.replace(t, ' ' + t)
		
			ee += x

# first pass to collect variable names, etc
#print ee
effect_file.parseString(ee)

generate_replacement_names()

id.setParseAction(idAction)

# second pass to replace names
replace_pass = True
scope_level = 0
effect_file.parseString(ee)

print '#pragma once'
for (k1,v1) in cur_scope.items():
	for (k2,v2) in v1.items():
		print '#define VAR_%s "%s"' % (k2, v2)

def print_output():
	# remove whitespace where possible and print the output
	res = 'char test1[] = '
	for stmt in fx_file:
		prev = ";"
		res += '"'
		for elem in stmt:
			compress_tokens = [x for x in "{}()[].,-+=;:"]
			cur = ""
			if not elem in compress_tokens and not prev in compress_tokens:
				cur += " "
			cur += elem
			res += cur
			prev = elem
		res += '"\n'
	res += ';'
	print res

print_output()
