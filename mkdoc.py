import os
import sys
import pathlib
import shutil

# Version that combines files.

# arg[0] is filename
# arg[1] is SolutionDir == C:\Dev\repos\Nebulator
# arg[2] is TargetDir == C:\Dev\repos\Nebulator\App\bin\net5.0-windows

# slate.css is good for "dark mode" documentation. It automatically switches to
# black-on-white and an inline table of contents when printed.
hdr = '''
<meta charset="utf-8" emacsmode="-*- markdown -*-">
<link rel="stylesheet" href="https://casual-effects.com/markdeep/latest/slate.css?">
'''

ftr = '''
<!-- Markdeep: -->
<style class="fallback">body{visibility:hidden} </style>
<script> markdeepOptions={tocStyle:'long'}; </script>
<script src="https://casual-effects.com/markdeep/latest/markdeep.min.js?" charset="utf-8" ></script>
'''

all_text = []


def do_one_file(srcfn):
    print('do_one_file', srcfn)
    with open(srcfn, "r") as srcf:
        all_text.append(srcf.read() + '\n')

if len(sys.argv) == 3:
    src_path = os.path.join(sys.argv[1], 'Doc')
    # out_path = os.path.join(sys.argv[2], 'Doc')
    out_path = sys.argv[2]
    # Ensure existence of output.
    pathlib.Path(out_path).mkdir(parents=True, exist_ok=True)

    # Special for the topmost.
    srcfn = os.path.join(sys.argv[1], 'README.md')
    # outfn = os.path.join(out_path, 'Main.md.html')
    do_one_file(srcfn)

    # Content files.
    for df in [ 'ScriptSyntax.md', 'ScriptApi.md', 'Internals.md', 'MusicDefinitions.md' ]:
        srcfn = os.path.join(src_path, df)
        # outfn = os.path.join(out_path, df + '.html')
        do_one_file(srcfn)

    # # Other files.
    # for df in [ 'marks.bmp' ]:
    #     srcfn = os.path.join(src_path, df)
    #     outfn = os.path.join(out_path, df)
    #     shutil.copyfile(srcfn, outfn)

    outfn = os.path.join(out_path, 'Main.md.html')

    with open(outfn, "w+") as outf:
        outf.write(hdr)
        for s in all_text:
            outf.write(s)
        outf.write(ftr)

else:
    print('Bad args!!')
