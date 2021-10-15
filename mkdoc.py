import os
import sys
import pathlib
import shutil

# Version that combines files.

# arg[0] is script filename
# arg[1] is SolutionDir == C:\Dev\repos\Nebulator

# slate.css is good for "dark mode" documentation. It automatically switches to
# black-on-white and an inline table of contents when printed.
hdr = '''
<meta charset="utf-8" emacsmode="-*- markdown -*-">
<link rel="stylesheet" href="https://casual-effects.com/markdeep/latest/slate.css?">
<!-- CET: make code stand out: -->
<style>.md code { color: #f3f }</style>
<!-- CET: toc needs to be wider and/or adjustable: TODO2 -->
<!-- <style>.md .longTOC { width:300px; }</style> -->
'''

# 
ftr = '''
<!-- Markdeep: -->
<style class="fallback">body{visibility:hidden} </style>
<script> markdeepOptions={tocStyle:'long'}; </script>
<script src="https://casual-effects.com/markdeep/latest/markdeep.min.js?" charset="utf-8" ></script>
# '''


all_text = []

if len(sys.argv) == 2:
    src_path = sys.argv[1]
    # src_path = os.path.join(sys.argv[1], 'DocFiles')
    out_path = sys.argv[1]
    # Ensure existence of output.
    pathlib.Path(out_path).mkdir(parents=True, exist_ok=True)

    # Content files.
    dfiles = [ 'Main.md', 'ScriptSyntax.md', 'ScriptApi.md', 'Internals.md', 'MusicDefinitions.md' ]
    for df in dfiles:
        srcfn = os.path.join(src_path, 'DocFiles', df)
        with open(srcfn, "r") as srcf:
            all_text.append(srcf.read() + '\n')

    outfn = os.path.join(out_path, dfiles[0] + '.html')
    with open(outfn, "w+") as outf:
        outf.write(hdr)
        for s in all_text:
            outf.write(s)
        outf.write(ftr)
else:
    print('Bad args!!')
    sys.exit(2)
