import os
import sys
import pathlib
import shutil

# arg[0] is filename
# arg[1] is SolutionDir == C:\Dev\repos\Nebulator
# arg[2] is TargetDir == C:\Dev\repos\Nebulator\App\bin\net5.0-windows

# hdr = '''<style> body { background-color:GhostWhite; font-family:Tahoma; font-size:14; } </style>'''
# ftr = '''
# <!-- Markdeep: -->
# <style class="fallback"> body { visibility:hidden; white-space:pre } </style>
# <script src="markdeep.min.js" charset="utf-8"></script>
# <script src="https://casual-effects.com/markdeep/latest/markdeep.min.js" charset="utf-8"></script>
# <script> window.alreadyProcessedMarkdeep || (document.body.style.visibility="visible")</script>
# '''

# slate.css is good for "dark mode" documentation. It automatically switches to
# black-on-white and an inline table of contents when printed.
hdr = '''
<meta charset="utf-8" emacsmode="-*- markdown -*-">
<link rel="stylesheet" href="https://casual-effects.com/markdeep/latest/slate.css?">
'''


ftr = '''
<style class="fallback">body{visibility:hidden} </style>
<script> markdeepOptions={tocStyle:'long'}; </script>
<!-- Markdeep: -->
<script src="https://casual-effects.com/markdeep/latest/markdeep.min.js?" charset="utf-8" ></script>
'''

ins = '''
(insert ScriptSyntax.md.html here)

(insert ScriptApi.md.html here)

(insert Internals.md.html here)

(insert MusicDefinitions.md.html here)
'''


def do_one_file(srcfn, outfn, main):
    print('copy', srcfn, 'to', outfn)
    with open(srcfn, "r") as srcf:
        with open(outfn, "w+") as outf:
            outf.write(hdr)
            outf.write(srcf.read())
            if main:
                outf.write(ins)
            outf.write(ftr)

if len(sys.argv) == 3:
    src_path = os.path.join(sys.argv[1], 'Doc')
    out_path = os.path.join(sys.argv[2], 'Doc')
    pathlib.Path(out_path).mkdir(parents=True, exist_ok=True)

    # Content files.
    for df in [ 'ScriptSyntax.md', 'ScriptApi.md', 'Internals.md', 'MusicDefinitions.md' ]:
        srcfn = os.path.join(src_path, df)
        outfn = os.path.join(out_path, df + '.html')
        do_one_file(srcfn, outfn, False)

    # Special for the topmost.
    srcfn = os.path.join(sys.argv[1], 'README.md')
    outfn = os.path.join(out_path, 'Main.md.html')
    do_one_file(srcfn, outfn, True)

    # Other files.
    for df in [ 'marks.bmp' ]:
        srcfn = os.path.join(src_path, df)
        outfn = os.path.join(out_path, df)
        shutil.copyfile(srcfn, outfn)

else:
    print('Bad args!!')
