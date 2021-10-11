import os
import sys
import pathlib

# arg0 - filename
# arg1 - SolutionDir
# arg2 - TargetDir

# C:\Dev\repos\Nebulator\App\bin\net5.0-windows -- TargetDir
# C:\Dev\repos\Nebulator -- SolutionDir

# TODO1 add nav stuff

hdr = '''
<style> body { background-color:GhostWhite; font-family:Tahoma; font-size:14; } </style>
'''

ftr = '''
<!-- Markdeep: -->
<style class="fallback"> body { visibility:hidden; white-space:pre } </style>
<script src="markdeep.min.js" charset="utf-8"></script>
<script src="https://casual-effects.com/markdeep/latest/markdeep.min.js" charset="utf-8"></script>
<script> window.alreadyProcessedMarkdeep || (document.body.style.visibility="visible")</script>
'''

doc_files = [ 'ScriptSyntax.md', 'ScriptApi.md', 'MusicDefinitions.md' ]

# print 'Number of arguments:', len(sys.argv), 'arguments.'
# print 'Argument List:', str(sys.argv)

if len(sys.argv) == 3:
    src_path = os.path.join(sys.argv[1], 'Doc')
    out_path = os.path.join(sys.argv[2], 'Doc')
    # pathlib.Path(out_path).mkdir(parents=True, exist_ok=True)

    for df in doc_files:
        srcfn = os.path.join(src_path, df)
        outfn = os.path.join(out_path, df + '.html')
        print('+++', srcfn, outfn)

        with open(srcfn, "r") as srcf:
            with open(outfn, "w+") as outf:
                outf.write(hdr)
                outf.write(srcf.read())
                outf.write(ftr)
else:
    print('bad args')
