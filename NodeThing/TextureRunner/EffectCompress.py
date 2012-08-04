# bnf from http://hl2glsl.codeplex.com/wikipage?title=HLSL%20BNF
# with a couple of tweaks/fixes

from pyparsing import alphas, alphanums, Word, Literal, Optional, Combine, OneOrMore, ZeroOrMore, Forward, Literal, Keyword, delimitedList, Group, Suppress, oneOf

import sys

def namedParseAction(name, only_name = False):
	def inner(str, location, token):
		if only_name:
			print ">> name: ", name
		else:
			print ">> name: ", name, "token: ", token
	return inner

C = Combine
O = Optional
W = Word
Z = ZeroOrMore
X = OneOrMore
L = Literal
K = Keyword
G = Group
S = Suppress

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

type = \
	(C(basic_type + (O(n2to4) + (O('x' + n2to4))))) ^ \
	K("vector") ^ \
	K("matrix") ^ \
	K("sampler") ^ K("sampler1D") ^ K("sampler2D") ^ K("sampler3D") ^ K("samplerCUBE") ^ K("sampler_state") ^ \
	K("texture") ^ K("Texture") ^ K("texture1D") ^ K("texture2D") ^ K("texture3D") ^ K("textureCUBE") ^ \
	K("void") ^ \
	id

in_out_inout = K('in') ^ K('out') ^ K('inout')

param = G(O(in_out_inout) + type + id + O(':' + semantical_parameters))
params = delimitedList(param)

term_tail = Forward()
factor = Forward()
term = factor + O(term_tail)
term_tail << (\
	'*' + term ^\
	'/' + term ^\
	'%' + term)

expression = Forward()
expression_tail = \
	'+' + expression ^\
	'-' + expression

expression << (term + O(expression_tail))

prefix_postfix_operators = L('++') ^ '--'
optional_suffix = O(('.' + xyzw) ^ ('.' + rgba) ^ ('.' + id))
id_composed = id + optional_suffix

atom = Forward()
func_call = type + L('(') + O(expression + Z(L(',') + expression)) + L(')')
ctor_call = func_call + optional_suffix

assignment_operator = \
	L('=') ^ L('+=') ^ L('-=') ^ L('*=') ^ L('/=') ^\
	L('%=') ^ L('<<=') ^ L('>>=') ^ L('&=') ^ L('|=') ^ L('^=')

initializers = expression

index = L('[') + (integer ^ id) + L(']')

variable_decl_atom = \
	O(storage_class) + O(type_modifier) + type + id + O(index) + \
	O(L(':') + semantical_parameters) + O(assignment_operator + initializers)

atom << (\
	number ^ \
	O(prefix_postfix_operators) + id_composed + O(prefix_postfix_operators) ^ \
	func_call ^ \
	ctor_call ^ \
	variable_decl_atom)

factor << (\
	atom ^ \
	L('(') + atom + L(')') + atom ^ \
	L('(') + expression + L(')') ^ \
	L('{') + O(expression + Z(L(',') + expression)) + L('}') ^ \
	L('<') + id + L('>'))

return_statement = K('return') + expression + L(';')

variable_assignment = id_composed + O(index) + assignment_operator + initializers + L(';')

flow_control_words = (K('stop') ^ K('continue') ^ K('break') ^ K('discard')) + L(';')

loop_attributes = \
	K('unroll') + K('(') + integer + K(')') ^ \
	K('loop')

while_attributes = loop_attributes
for_attributes = loop_attributes

function_body = Forward()
statement_scope = \
	L('{') + function_body + L('}') ^ \
	function_body
	
comparison_operators = L('<') ^ L('>') ^ L('==') ^ L('!=') ^ L('<=') ^ L('>=')

condition = expression + comparison_operators + expression
	
while_statement = O(while_attributes) + K('while') + L('(') + condition + L(')') + statement_scope

do_statement = K('do') + statement_scope + K('while') + L('(') + condition + L(')') + L(';')
	
if_attributes = K('flatten') ^ K('branch')
if_statement = \
	O(if_attributes) + K('if') + L('(') + condition + L(')') + \
		statement_scope + \
	O(K('else') + \
		statement_scope)
	
for_statement = \
	O(for_attributes) + K('for') + L('(') + \
		Z(variable_assignment ^ expression) + ';' + \
		O(condition) + L(';') + \
		O((expression) ^ id_composed + O(index) + assignment_operator + initializers) + ')' + \
			statement_scope
		
switch_attributes = K('call') ^ K('forcecase') ^ K('branch') ^ K('flatten')
switch_statement = \
	switch_attributes + K('switch') + L('(') + expression + L(')') + L('{') + \
		X((K('case') + integer ^ K('default')) + L(':') + O(statement_scope)) + \
	L('}')

variable_decl = variable_decl_atom + L(';')

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
	K("sampler") ^ K("sampler1D") ^ K("sampler2D") ^ K("sampler3D") ^ K("samplerCUBE")

sampler_decl = sampler_type + id + L('=') + type + L('{') + \
	Z(variable_assignment) + \
	L('}') + ';'
	
function_decl = \
	O(storage_class) + O(type_modifier) + \
	type + id + S(L('(')) + G(O(params)) + S(L(')')) + O(S(L(':')) + semantical_parameters) + \
		S(L('{') + \
			function_body + \
		L('}'))
		
technique_decl = \
	S(K('technique')) + id + S(L('{')) + \
		Z( \
			G(S(K('pass')) + id + S(L('{')) + \
				Z(\
					id + S(L('=')) + O(K('compile') + id) + (id + L('()') ^ expression) + L(';') \
				)) +\
			S(L('}')) \
		) +\
	S(L('}'))

struct_var = G(type + id + S(L(':')) + semantical_parameters) + S(';')
struct_decl = \
	S(K('struct')) + id + S(L('{')) + \
		X(struct_var) + \
	S(L('}') + ';')
	

effect_file = X(
	function_decl ^ \
	variable_decl ^ \
	struct_decl ^ \
	technique_decl ^ \
	sampler_decl)

#struct_decl.setParseAction(parseAction)
#var_decl.setParseAction(parseAction)
function_decl.setParseAction(namedParseAction("func-decl"))
struct_decl.setParseAction(namedParseAction("struct_decl"))
#type.setParseAction(namedParseAction("type"))
#id.setParseAction(namedParseAction("id"))
#variable_decl.setParseAction(namedParseAction("variable_decl"))
#variable_assignment.setParseAction(namedParseAction("variable_assignment"))
technique_decl.setParseAction(namedParseAction("technique_decl"))
#sampler_decl.setParseAction(namedParseAction("sampler_decl"))
#ctor_call.setParseAction(namedParseAction("ctor_call"))
#atom.setParseAction(namedParseAction("atom"))
#expression.setParseAction(namedParseAction("expression"))
#func_call.setParseAction(namedParseAction("func_call"))

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

effect_file.parseString(ee)