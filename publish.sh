#!/usr/bin/env bash
# Выкладывает собранный установщик в Releases репозитория через GitHub API.
# Нужна переменная окружения GH_TOKEN (Contents: write). Токен нигде не сохраняется.
set -e
cd "$(dirname "$0")"

REPO="zaur123mustafaev-collab/MrBean-setup"
TAG="${1:?usage: GH_TOKEN=... bash publish.sh setup-v1.0.0}"
NAME="${TAG#setup-v}"
: "${GH_TOKEN:?set GH_TOKEN to a GitHub token with Contents:write on $REPO}"

if [ ! -f MrBeanVisualsSetup.exe ]; then
	echo "нет MrBeanVisualsSetup.exe — сначала bash build.sh" >&2
	exit 1
fi

sha() { certutil -hashfile "$1" SHA256 | sed -n 2p | tr -d ' \r'; }
sha MrBeanVisualsSetup.exe > SHA256SUMS.exe
printf '%s  MrBeanVisualsSetup.exe\n' "$(sha MrBeanVisualsSetup.exe)" > SHA256SUMS.txt
rm -f SHA256SUMS.exe

AUTH="Authorization: Bearer $GH_TOKEN"
API="https://api.github.com/repos/$REPO"

if ! curl -sf -H "$AUTH" "$API/git/refs/tags/$TAG" -o /dev/null; then
	curl -sf -H "$AUTH" "https://api.github.com/repos/$REPO/commits?sha=main" -o /tmp/mbc.json
	SHA=$(python -c "import json;print(json.load(open('/tmp/mbc.json'))[0]['sha'])")
	curl -sf -H "$AUTH" -X POST "$API/git/refs" \
		-d "{\"ref\":\"refs/tags/$TAG\",\"sha\":\"$SHA\"}" -o /dev/null
	echo "тег $TAG поставлен на $SHA"
fi

if curl -sf -H "$AUTH" "$API/releases/tags/$TAG" -o /dev/null; then
	echo "релиз для $TAG уже есть"
else
	BODY=$(printf '{"tag_name":"%s","name":"MrBean Visuals Setup %s","body":"Установщик лаунчера. До установки проверьте SHA-256 по SHA256SUMS.txt.","draft":false,"prerelease":false}' "$TAG" "$NAME")
	curl -sf -H "$AUTH" -H "Content-Type: application/json" -X POST "$API/releases" -d "$BODY" -o /tmp/mbr.json
	REL=$(python -c "import json;print(json.load(open('/tmp/mbr.json'))['upload_url'].split('{')[0])")
	echo "релиз создан: $REL"
fi

for f in MrBeanVisualsSetup.exe SHA256SUMS.txt; do
	curl -sf -H "$AUTH" -H "Content-Type: application/octet-stream" \
		--data-binary "@$f" "$REL?name=$f" -o /dev/null
	echo "залит: $f"
done

echo "https://github.com/$REPO/releases/tag/$TAG"
