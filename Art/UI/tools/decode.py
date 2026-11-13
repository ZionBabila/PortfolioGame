import base64, html, json, os, re, sys

dom_path, out_dir = sys.argv[1], sys.argv[2]
dom = open(dom_path, encoding="utf-8").read()
m = re.search(r'<pre id="out">(.*?)</pre>', dom, re.S)
layers = json.loads(html.unescape(m.group(1)))
os.makedirs(out_dir, exist_ok=True)
manifest = []
for i, l in enumerate(layers):
    fn = f"{i:02d}_{l['group'].replace('/', '-')}_{l['name']}.png"
    with open(os.path.join(out_dir, fn), "wb") as f:
        f.write(base64.b64decode(l["png"].split(",", 1)[1]))
    manifest.append({k: l[k] for k in ("group", "name", "visible", "blend", "opacity")} | {"file": os.path.join(out_dir, fn)})
json.dump(manifest, open(os.path.join(out_dir, "manifest.json"), "w"), indent=1)
print(len(manifest), "layers")
