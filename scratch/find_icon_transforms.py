import re
import sys

def parse_unity(file_path):
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    docs = content.split('---')
    game_objects = {}
    rect_transforms = {}
    
    for doc in docs:
        if not doc.strip():
            continue
        
        # Extract FileID
        file_id_match = re.search(r'!u!\d+ &(\d+)', doc)
        if not file_id_match:
            continue
        file_id = file_id_match.group(1)
        
        if 'GameObject:' in doc:
            name_match = re.search(r'm_Name: (.*)', doc)
            if name_match:
                name = name_match.group(1).strip()
                # Find RectTransform component
                # m_Component:
                # - component: {fileID: 12345}
                components = re.findall(r'- component: \{fileID: (\d+)\}', doc)
                game_objects[file_id] = {'name': name, 'components': components}
                
        elif 'RectTransform:' in doc:
            # Extract position data
            pos_x = re.search(r'm_LocalPosition: \{x: ([^,]+),', doc)
            pos_y = re.search(r'm_LocalPosition: \{.*y: ([^,]+),', doc)
            anc_pos_x = re.search(r'm_AnchoredPosition: \{x: ([^,]+),', doc)
            anc_pos_y = re.search(r'm_AnchoredPosition: \{.*y: ([^,]+)\}', doc)
            
            # Find which GameObject it belongs to
            go_match = re.search(r'm_GameObject: \{fileID: (\d+)\}', doc)
            go_id = go_match.group(1) if go_match else None
            
            rect_transforms[file_id] = {
                'go_id': go_id,
                'pos_x': pos_x.group(1) if pos_x else None,
                'pos_y': pos_y.group(1) if pos_y else None,
                'anc_pos_x': anc_pos_x.group(1) if anc_pos_x else None,
                'anc_pos_y': anc_pos_y.group(1) if anc_pos_y else None,
                'raw': doc
            }

    # Match GameObjects to their RectTransforms
    for go_id, go_info in game_objects.items():
        name = go_info['name']
        if 'hat' in name.lower() or 'fish' in name.lower() or 'icon' in name.lower() or 'button' in name.lower() or 'display' in name.lower():
            # Check components for RectTransform
            for comp_id in go_info['components']:
                if comp_id in rect_transforms:
                    rt = rect_transforms[comp_id]
                    print(f"Name: {name} (FileID: {go_id})")
                    print(f"  AnchoredPos: ({rt['anc_pos_x']}, {rt['anc_pos_y']})")
                    print(f"  LocalPos: ({rt['pos_x']}, {rt['pos_y']})")
                    print(f"  RT FileID: {comp_id}")
                    print("-" * 40)

if __name__ == '__main__':
    parse_unity(r'e:\PanicAtThePond\Assets\Scenes\Dash.unity')
