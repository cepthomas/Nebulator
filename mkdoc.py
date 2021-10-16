import os
import sys
import pathlib
import shutil

# Combine md files into single html with toc. Assumes cwf is the repo of interest.
# Uses markdeep slate.css - good for "dark mode" documentation or apidoc for white.

repo_path = os.getcwd()
# print('>>>>>', os.getcwd())

# arg[0] is script filename
# arg[1] is SolutionDir == C:\Dev\repos\Nebulator

hdr = '''
<meta charset="utf-8" emacsmode="-*- markdown -*-">
<link rel="stylesheet" href="https://casual-effects.com/markdeep/latest/apidoc.css?">
'''

# <link rel="stylesheet" href="https://casual-effects.com/markdeep/latest/apidoc.css?">

# Modifications.
hdr_mod = '''
<!-- make code stand out (for slate.css): -->
<style>.md code { color: #f3f }</style>
'''
# <!-- toc needs to be wider and/or adjustable: TODO2 doesnt work right -->
# <style>.md .longTOC { width:300px; }</style>

# 
ftr = '''
<!-- Markdeep: -->
<style class="fallback">body{visibility:hidden} </style>
<script> markdeepOptions={tocStyle:'long'}; </script>
<script src="https://casual-effects.com/markdeep/latest/markdeep.min.js?" charset="utf-8" ></script>
# '''

all_text = []

# Ensure existence of output.
# pathlib.Path(repo_path).mkdir(parents=True, exist_ok=True)

# Content files.
dfiles = [ 'Nebulator.md', 'ScriptSyntax.md', 'ScriptApi.md', 'Internals.md', 'MusicDefinitions.md' ]
for df in dfiles:
    srcfn = os.path.join(repo_path, 'DocFiles', df)
    with open(srcfn, "r") as srcf:
        all_text.append(srcf.read() + '\n')

# Output to
outfn = os.path.join(repo_path, dfiles[0] + '.html')
with open(outfn, "w+") as outf:
    outf.write(hdr)
    # outf.write(hdr_mod)
    for s in all_text:
        outf.write(s)
    outf.write(ftr)
