// bnf from http://hl2glsl.codeplex.com/wikipage?title=HLSL%20BNF

from pyparsing import alphas, alphanums, Word, Literal, Optional, Combine, OneOrMore, ZeroOrMore

def parseAction(str, location, token):
	#print "str: ", str
	print "loc: ", location
	print "token: ", token

def bah():
	semantic = Word('POSITION') ^ Word('TEXCOORD')
	type = Word('matrix') ^ Word('float') ^ Word('float2') ^ Word('vector')
	digits = "0123456789"

	varDecl = type + id + ';';
	tt = type + id + ':' + semantic + ';'
	tts = OneOrMore(tt)
	struct = 'struct' + id + '{' + tts + '}' + ';'

#expr

#assignment = '=' expr
#statement = varDecl | assignment

#func = id + id + '(' + id + id + ')' + '{' + statements + '}' + ';'

#varDecl.setParseAction(parseAction)
#tt.setParseAction(parseAction)

storage_class = Word("extern") ^ "static" ^ "nointerpolation" ^ \
	"shared" ^ "uniform" ^ "volatile"
type_modifier = Word("const") ^ "row_major" ^ "column_major"
	

C = Combine
O = Optional

input_sem_params = Word('BINORMAL') ^ 'BLENDINDICES' ^ 'BLENDWEIGHT' ^ 'NORMAL' ^ \
	'POSITION' ^ 'TANGENT' ^ 'VFACE' ^ 'VPOS'
	
output_sem_params = Word('FOG') ^ 'TESSFACTOR' ^ 'DEPTH'

x0to9 = Word("0123456789", exact=1)
x2to4 = Word("234", exact=1)

in_out_sem_params = \
	C(Word('POSITION') + O(x0to9)) ^ \
	C(Word('TEXCOORD') + O(x0to9)) ^ \
	C(Word('TEXUNIT') + O(x0to9)) ^ \
	C(Word('COLOR') + O(x0to9)) ^ \
	Word('PSIZE')
	
semantic_matrices = \
	C(Word('WORLD') + O(Word('I')) + O(Word('T'))) ^ \
	C(Word('VIEW') + O(Word('I')) + O(Word('T'))) ^ \
	C(Word('PROJ') + O(Word('I')) + O(Word('T'))) ^ \
	C(Word('WORLDVIEW') + O(Word('I')) + O(Word('T'))) ^ \
	C(Word('WORLDPROJ') + O(Word('I')) + O(Word('T'))) ^ \
	C(Word('VIEWPROJ') + O(Word('I')) + O(Word('T'))) ^ \
	C(Word('WORLDVIEWPROJ') + O(Word('I')) + O(Word('T')))
	

semantical_parameters = \
	input_sem_params ^ \
	output_sem_params ^ \
	in_out_sem_params ^ \
	semantic_matrices
	

id = C(Word(alphas + "_") + ZeroOrMore(Word(alphanums + "_")))

basic_type = Word("float") ^ "int" ^ "half" ^ "double" ^ "bool"

type = \
	(C(basic_type + (O(x2to4) + (O('x' + x2to4))))) ^ \
	"vector" ^ \
	"matrix" ^ \
	"sampler" ^ "sampler1D" ^ "sampler2D" ^ "sampler3D" ^ "samplerCUBE" ^ "sampler_state" ^ \
	"texture" ^ "texture1D" ^ "texture2D" ^ "texture3D" ^ "textureCUBE" ^ \
	"void" ^ \
	id

struct_var = type + id + Word(':') + semantical_parameters + Word(';')
	
struct_declaration = Word('struct') + id + Word('{') + \
	OneOrMore(struct_var) + \
	Word('}') + ';'


var_decl = type + id + Word(';')
effect_file = OneOrMore(var_decl ^ struct_declaration)

#struct_declaration.setParseAction(parseAction)
var_decl.setParseAction(parseAction)
struct_var.setParseAction(parseAction)

e = open("test2.fx").readlines()
ee = ""
for x in e:
	v = x.strip()
	if len(v) > 0:
		ee += v

print effect_file.parseString(ee)
