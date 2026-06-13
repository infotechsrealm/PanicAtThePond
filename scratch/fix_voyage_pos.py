import re

def fix_dash():
    path = r'e:\PanicAtThePond\Assets\Scenes\Dash.unity'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Find VoyageDiagramAnchoredPosition and replace y: 120 with y: 70
    content = re.sub(r'VoyageDiagramAnchoredPosition: \{x: -110, y: 120\}', r'VoyageDiagramAnchoredPosition: {x: -110, y: 70}', content)
    
    with open(path, 'w', encoding='utf-8', newline='\n') as f:
        f.write(content)

fix_dash()
