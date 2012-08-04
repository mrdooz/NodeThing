# bnf from http://hl2glsl.codeplex.com/wikipage?title=HLSL%20BNF
# with a couple of tweaks/fixes

from pyparsing import alphas, alphanums, Word, Literal, Optional, Combine, OneOrMore, ZeroOrMore, Forward

import sys

def namedParseAction(name):
	def inner(str, location, token):
		print ">> name: ", name, "token: ", token
	return inner

C = Combine
O = Optional
W = Word

id = C(W(alphas + "_") + ZeroOrMore(W(alphanums + "_")))
digit = W('0123456789', exact=1)
E = W('eE', exact=1)
number = \
	OneOrMore(digit) ^ \
	ZeroOrMore(digit) + W('.') + OneOrMore(digit) + O(E + OneOrMore(digit)) + O(W('f'))
integer = OneOrMore(digit)
n1to4 = W("1234", exact=1)
n2to4 = W("234", exact=1)
rgba = W("rgba")
xyzw = W("xyzw")

storage_class = W('extern') ^ 'static' ^ 'nointerpolation' ^ 'shared' ^ 'uniform' ^'volatile'
type_modifier = W('const') ^ 'row_major' ^ 'column_major'

input_sem_params = W('BINORMAL') ^ 'BLENDINDICES' ^ 'BLENDWEIGHT' ^ 'NORMAL' ^ 'POSITION' ^  'TANGENT' ^ 'VFACE' ^ 'VPOS'

output_sem_params = W('FOG') ^ 'TESSFACTOR' ^'DEPTH'

in_out_sem_params = \
	C(W('POSITION') + O(digit)) ^ \
	C(W('TEXCOORD') + O(digit)) ^ \
	C(W('TEXUNIT') + O(digit)) ^ \
	C(W('COLOR') + O(digit)) ^ \
	W('PSIZE')
	
semantic_matrices = \
	C(W('WORLD') + O(W('I')) + O(W('T'))) ^ \
	C(W('VIEW') + O(W('I')) + O(W('T'))) ^ \
	C(W('PROJ') + O(W('I')) + O(W('T'))) ^ \
	C(W('WORLDVIEW') + O(W('I')) + O(W('T'))) ^ \
	C(W('WORLDPROJ') + O(W('I')) + O(W('T'))) ^ \
	C(W('VIEWPROJ') + O(W('I')) + O(W('T'))) ^ \
	C(W('WORLDVIEWPROJ') + O(W('I')) + O(W('T')))
	

semantical_parameters = \
	input_sem_params ^ \
	output_sem_params ^ \
	in_out_sem_params ^ \
	semantic_matrices

basic_type = W('float') ^ 'int' ^ 'half' ^ 'double' ^ 'bool'

type = \
	(C(basic_type + (O(n2to4) + (O('x' + n2to4))))) ^ \
	"vector" ^ \
	"matrix" ^ \
	"sampler" ^ "sampler1D" ^ "sampler2D" ^ "sampler3D" ^ "samplerCUBE" ^ "sampler_state" ^ \
	"texture" ^ "Texture" ^ "texture1D" ^ "texture2D" ^ "texture3D" ^ "textureCUBE" ^ \
	"void" ^ \
	id

in_out_inout = W('in') ^ 'out' ^ 'inout'

param = O(in_out_inout) + type + id
params = Forward()
params << (param ^ (param + W(',') + params))

function_body = Forward()
term_tail = Forward()
factor = Forward()
term = factor + O(term_tail)
term_tail << ((W('*') + term) | (W('/') + term) | (W('%') + term))

expression = Forward()
expression_tail = (W('+') + expression) ^ (W('-') + expression)
expression << (term + O(expression_tail))

prefix_postfix_operators = W('++') ^ W('--')
optional_dot = O((W('.') + xyzw) ^ (W('.') + rgba) ^ (W('.') + id))
id_composed = id + optional_dot

ctor_call = \
	type + W('(') + expression + ZeroOrMore(W(',') + expression) + W(')') + \
	optional_dot

assignment_operator = W("=") ^ W("+=") ^ W("-=") ^ W("*=") ^ W("/=") ^ W("%=") ^\
	W("<<=") ^ W(">>=") ^ W("&=") ^ W("|=") ^ W("^=")
	
initializers = expression
	
index = W('[') + integer + W(']')
	
variable_decl_atom = \
	O(storage_class) + O(type_modifier) + type + id + O(index) + \
	O(W(':') + semantical_parameters) + O(assignment_operator + initializers)

atom = \
	number ^ \
	O(prefix_postfix_operators) + id_composed + O(prefix_postfix_operators) ^ \
	ctor_call ^ \
	variable_decl_atom

factor << (atom ^ (W('(') + expression + W(')')))
	
return_statement = W('return') + expression + W(';')

variable_assignment = id_composed + O(index) + assignment_operator + initializers + ';'
	
flow_control_words = (W('stop') ^ W('continue') ^ W('break') ^ W('discard')) + W(';')

loop_attributes = \
	W('unroll') + W('(') + integer + W(')') ^ \
	W('loop')

while_attributes = loop_attributes
for_attributes = loop_attributes

statement_scope = \
	W('{') + function_body + W('}') ^ \
	function_body
	
comparison_operators = W('<') ^ '>' ^ '==' ^ '!=' ^ '<=' ^ '>='

condition = expression + comparison_operators + expression
	
while_statement = O(while_attributes) + W('while') + W('(') + condition + W(')') + statement_scope

do_statement = W('do') + statement_scope + W('while') + W('(') + condition + W(')') + W(';')
	
if_attributes = W('flatten') ^ 'branch'
if_statement = \
	O(if_attributes) + W('if') + W('(') + condition + ')' + statement_scope + \
	O(W('else') + statement_scope)
	
for_statement = \
	O(for_attributes) + W('for') + W('(') + \
		ZeroOrMore(variable_assignment ^ expression) + ';' + \
		Optional(condition) + ';' + \
		Optional((expression) ^ id_composed + O(index) + assignment_operator + initializers) + ';' + \
		statement_scope
		
switch_attributes = W('call') ^ 'forcecase' ^ 'branch' ^ 'flatten'
switch_statement = \
	switch_attributes + W('switch') + '(' + expression + W(')') + '{' + \
		OneOrMore((W('case') + integer ^ W('default')) + W(':') + O(statement_scope)) + W('}')

variable_decl = variable_decl_atom + W(';')
		
	
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
	
function_body << ZeroOrMore(statement)

sampler_decl = W('sampler') + id + W('=') + type + W('{') + \
	ZeroOrMore(variable_assignment) + \
	W('}') + ';'
	
function_decl = \
	O(storage_class) + O(type_modifier) + \
	type + id + W('(') + O(params) + W(')') + O(W(':') + semantical_parameters) + \
		W('{') + \
			function_body + \
		W('}')
		
technique_decl = \
	W('technique') + id + W('{') + \
		ZeroOrMore( \
			W('pass') + id + W('{') + \
				ZeroOrMore(\
					id + W('=') + O(W('compile') + id) + (id + W('()') ^ expression) + W(';') \
				) +\
			W('}') \
		) +\
	W('}')

technique_decl2 = \
	W('technique') + id + W('{') + W('pass') + id + W('{') + W('}') + W('}')

struct_var = type + id + W(':') + semantical_parameters + W(';')
struct_decl = \
	W('struct') + id + W('{') + \
		OneOrMore(struct_var) + \
	W('}') + ';'
	

effect_file = OneOrMore(
	function_decl ^ \
	variable_decl ^ \
	struct_decl ^ \
	technique_decl ^ \
	sampler_decl)

#struct_decl.setParseAction(parseAction)
#var_decl.setParseAction(parseAction)
#struct_var.setParseAction(parseAction)
function_decl.setParseAction(namedParseAction("func-decl"))
#type.setParseAction(namedParseAction("type"))
#id.setParseAction(namedParseAction("id"))
variable_decl.setParseAction(namedParseAction("variable_decl"))
variable_assignment.setParseAction(namedParseAction("variable_assignment"))
technique_decl.setParseAction(namedParseAction("technique_decl"))
sampler_decl.setParseAction(namedParseAction("sampler_decl"))
#atom.setParseAction(namedParseAction("atom"))
#expression.setParseAction(namedParseAction("expression"))

if len(sys.argv) < 2:
	exit(1)

e = open(sys.argv[1]).readlines()
ee = ""
for x in e:
	# skip lines that only consist of whitespace
	if len(x.strip()) > 0:
		ee += x

effect_file.parseString(ee)
