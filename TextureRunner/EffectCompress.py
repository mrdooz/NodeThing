# bnf from http://hl2glsl.codeplex.com/wikipage?title=HLSL%20BNF
# with a couple of tweaks/fixes

from pyparsing import alphas, alphanums, Word, Literal, Optional, Combine, OneOrMore, ZeroOrMore, Forward, \
		Literal, Keyword, delimitedList, Group, Suppress, oneOf, nestedExpr

import sys
from collections import defaultdict

scopeLevel = 0

cur_scope = defaultdict(dict)
replace_pass = False

def enterFunc(str, loc, tok):
	global scopeLevel
	scopeLevel += 1

def leaveFunc(str, loc, tok):
	pass
#	global scopeLevel
#	scopeLevel -= 1

def idAction(str, loc, tok):
	# check if the id should be replaced
	name = tok[0]
#	print cur_scope[scopeLevel], tok
#	if name in cur_scope[scopeLevel]:
#		print "local scope: ", tok, str[loc-20:loc+20]

def declVariable(str, loc, tok):
	name = tok[1]
	cur_scope[scopeLevel][name] = name 
#	print ">> var: ", name, " level: ", scopeLevel

def typeAction(str, location, token):
	print "type >> ", token

def namedParseAction(name, only_name = False):
	def inner(str, location, token):
		if only_name:
			print ">> name: ", name
		else:
			print ">> name: ", name, "token: ", token
	return inner

def generate_replacement_names():
	curid = "a"
	for i in range(scopeLevel):
		for (k,v) in cur_scope[i].items():
			cur_scope[i][k] = curid
			o = ord(curid[-1])
			if o < ord('z'):
				curid = curid[:len(curid)-1] + chr(o+1)
			else:
				curid = "a" * (len(curid) + 1)

	print cur_scope
	
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
C = lambda x : x
G = lambda x : x

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

param = G(O(in_out_inout) + ftype + id + O(':' + semantical_parameters))
params = delimitedList(param)

term_tail = Forward()
factor = Forward()
term = factor + O(term_tail)
term_tail << (\
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
id_composed = G(id + optional_suffix)

atom = Forward()
func_call = id + nestedExpr('(', ')', delimitedList(expression))
ctor_call = func_call + optional_suffix

assignment_operator = \
	L('=') | '+=' | '-=' | '*=' | '/=' | '%=' | '<<=' | '>>=' | '&=' | '|=' | '^='

initializers = expression

index = nestedExpr('[', ']', integer ^ id)

variable_decl_atom = \
	O(storage_class) + O(type_modifier) + ftype + id + O(index) + \
	O(':' + semantical_parameters) + O(assignment_operator + initializers)

atom << (\
	number ^ \
	O(prefix_postfix_operators) + id_composed + O(prefix_postfix_operators) ^ \
	func_call ^ \
	ctor_call ^ \
	nestedExpr('{', '}', delimitedList(expression)))

factor << (\
	O(nestedExpr('(', ')', ftype)) + atom | \
	nestedExpr('(', ')', expression) | \
	nestedExpr('<', '>', id))

return_statement = K('return') + expression + S(';')

variable_assignment = id_composed + O(index) + assignment_operator + initializers + S(';')

flow_control_words = (K('stop') | K('continue') | K('break') | K('discard')) + S(';')

loop_attributes = \
	K('unroll') + nestedExpr('(', ')', integer) | \
	K('loop')

while_attributes = loop_attributes
for_attributes = loop_attributes

function_body = Forward()
statement_scope = \
	nestedExpr('{', '}', function_body) | \
	function_body
	
comparison_operators = L('<=') | '>=' | '<' | '>' | '==' | '!='

condition = expression + comparison_operators + expression
	
while_statement = O(while_attributes) + K('while') + nestedExpr('(', ')', condition) + statement_scope

do_statement = K('do') + statement_scope + K('while') + nestedExpr('(', ')', condition) + S(';')
	
if_attributes = K('flatten') | K('branch')
if_statement = \
	O(if_attributes) + K('if') + nestedExpr('(', ')', condition) + \
		statement_scope + \
	O(K('else') + \
		statement_scope)
	
for_statement = \
	O(for_attributes) + K('for') + '(' + \
		Z(variable_assignment ^ expression) + ';' + \
		O(condition) + ';' + \
		O((expression) ^ id_composed + O(index) + assignment_operator + initializers) + ')' + \
			statement_scope
		
switch_attributes = K('call') | K('forcecase') | K('branch') | K('flatten')
switch_statement = \
	switch_attributes + K('switch') + nestedExpr('(', ')', expression) + '{' + \
		X((K('case') + integer ^ K('default')) + ':' + O(statement_scope)) + \
	'}'

variable_decl = variable_decl_atom + S(';')

statement = \
	return_statement ^ \
	variable_assignment ^ \
	variable_decl ^ \
	flow_control_words ^ \
	while_statement ^ \
	do_statement ^ \
	if_statement ^ \
	for_statement ^ \
	switch_statement

function_body << Z(statement)

sampler_type = \
	K("sampler1D") | K("sampler2D") | K("sampler3D") | K("samplerCUBE") | K("sampler")

sampler_decl = sampler_type + id + S('=') + ftype + S('{') + \
	Z(variable_assignment) + \
	S('}' + ';')

function_decl = \
	O(storage_class) + O(type_modifier) + \
	ftype + id + nestedExpr('(', ')', params) + O(S(':') + semantical_parameters) + \
		S(L('{').setParseAction(enterFunc) + \
			function_body + \
		L('}').setParseAction(leaveFunc))
		
technique_decl2 = \
	S(K('technique')) + id + S('{') + \
		Z( \
			G(S(K('pass')) + id + S('{') + \
				Z(\
					id + S('=') + O(K('compile') + id) + (id + '()' ^ expression) + S(';') \
				)) +\
			S('}') \
		) +\
	S('}')

technique_decl = \
	S(K('technique')) + id + S('{') + \
		Z( \
			G(S(K('pass')) + id + S('{') + \
				DelimitedList(id + S('=') + O(K('compile') + id) + (id + '()' ^ expression,  ';')) + \
			S('}') \
		) +\
	S('}')

struct_var = G(ftype + id + S(':') + semantical_parameters) + S(';')
struct_decl = \
	S(K('struct')) + id + S('{') + \
		X(struct_var) + \
	S('}' + ';')
	

fx_file = []

def funcAction(str, loc, tok):
	global fx_file
	fx_file.append(tok)

def varAction(str, loc, tok):
	global fx_file
	fx_file.append(tok)

def structAction(str, loc, tok):
	global fx_file
	fx_file.append(tok)

def techniqueAction(str, loc, tok):
	global fx_file
	fx_file.append(tok)

def samplerAction(str, loc, tok):
	global fx_file
	fx_file.append(tok)


effect_file = X(
	function_decl.setParseAction(funcAction) ^ \
	variable_decl.setParseAction(varAction) ^ \
	struct_decl.setParseAction(structAction) ^ \
	technique_decl.setParseAction(techniqueAction) ^ \
	sampler_decl.setParseAction(samplerAction))

def action_variable_decl(str, loc, tok):
	print ">> var", tok, " at ", loc 

#struct_decl.setParseAction(parseAction)
#function_decl.setParseAction(namedParseAction("func-decl"))
#struct_decl.setParseAction(namedParseAction("struct_decl"))
#type.setParseAction(namedParseAction("type"))
#id.setParseAction(namedParseAction("id"))
variable_decl.setParseAction(declVariable)
#variable_assignment.setParseAction(namedParseAction("variable_assignment"))
#technique_decl.setParseAction(namedParseAction("technique_decl"))
#sampler_decl.setParseAction(namedParseAction("sampler_decl"))
#ctor_call.setParseAction(namedParseAction("ctor_call"))
#atom.setParseAction(namedParseAction("atom"))
#expression.setParseAction(namedParseAction("expression"))
#func_call.setParseAction(namedParseAction("func_call"))
#type.setParseAction(typeAction)

id.setParseAction(idAction)

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

#			for t in ['=']:
#				x = x.replace(t, ' ' + t + ' ')
		
			ee += x

#print ee
#variable_decl.validate()
#variable_decl.setDebug(True)
#function_decl.parseString(ee)
#print ee

# first pass to collect variable names, etc
effect_file.parseString(ee)

#generate_replacement_names()

for f in fx_file:
	print f 

# second pass to replace names

#print cur_scope
