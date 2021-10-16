import os
import sys
import pathlib
import shutil

# Combine md files into single html with toc. Assumes cwd is the repo of interest.
# Markdeep styles: slate.css for dark mode or apidoc.css for light.

hdr = '''
<meta charset="utf-8" emacsmode="-*- markdown -*-">
<link rel="stylesheet" href="https://casual-effects.com/markdeep/latest/apidoc.css?">
'''

# Modifications (for slate.css).
hdr_mod = '''
<!-- make code stand out: -->
<style>.md code { color: #f3f }</style>
'''
# <!-- toc needs to be wider and/or adjustable: TODO doesnt work right -->
# <style>.md .longTOC { width:300px; }</style>

ftr = '''
<!-- Markdeep: -->
<style class="fallback">body{visibility:hidden} </style>
<script> markdeepOptions={tocStyle:'long'}; </script>
<script src="https://casual-effects.com/markdeep/latest/markdeep.min.js?" charset="utf-8" ></script>
# '''

all_text = []

repo_path = os.getcwd()

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
