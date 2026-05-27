import re
with open(r"C:\Users\rishi\.gemini\antigravity\brain\8a826d45-0cb7-4a16-91c0-8e49e5df24ba\.system_generated\tasks\task-114.log", 'r') as f:
    text = f.read()

matches = re.findall(r"Matched (.+\.png) -> Frame (\d+)", text)
frame_counts = {}
for name, frame in matches:
    frame = int(frame)
    frame_counts[frame] = frame_counts.get(frame, 0) + 1

print(f"Total mappings: {len(matches)}")
print(f"Unique frames matched: {len(frame_counts)}")
duplicates = {k: v for k, v in frame_counts.items() if v > 1}
print(f"Duplicates: {duplicates}")
