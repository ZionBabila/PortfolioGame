import base64, html, json, os, re, sys
dom, out = sys.argv[1], sys.argv[2]
m = re.search(r'<pre id="out">(.*?)</pre>', open(dom, encoding="utf-8").read(), re.S)
os.makedirs(out, exist_ok=True)
for s in json.loads(html.unescape(m.group(1))):
    open(os.path.join(out, s["name"] + ".png"), "wb").write(base64.b64decode(s["png"].split(",", 1)[1]))
    print(s["name"])
