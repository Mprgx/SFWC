import json
import os
import sys
from collections import Counter

def norm(s: str | None) -> str:
    return (s or "").strip().lower()

def read_record(obj: dict) -> dict:
    return obj.get("record") or obj

def main(path: str):
    if not os.path.exists(path):
        print(f"File not found: {path}")
        sys.exit(1)

    username_counts = Counter()
    email_counts = Counter()
    phone_counts = Counter()

    with open(path, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            try:
                obj = json.loads(line)
            except Exception:
                continue

            rec = read_record(obj)
            username_counts[norm(rec.get("username"))] += 1
            email_counts[norm(rec.get("email"))] += 1
            phone_counts[norm(rec.get("phone"))] += 1

    out_dir = os.path.join(os.path.dirname(path), "SplitResults")
    os.makedirs(out_dir, exist_ok=True)

    base_name = os.path.splitext(os.path.basename(path))[0]

    out_user = os.path.join(out_dir, base_name + ".by_username.jsonl")
    out_email = os.path.join(out_dir, base_name + ".by_email.jsonl")
    out_unknown = os.path.join(out_dir, base_name + ".unknown.jsonl")


    written_user = written_email = written_phone = written_unknown = 0

    with open(path, "r", encoding="utf-8") as f, \
         open(out_user, "w", encoding="utf-8") as fu, \
         open(out_email, "w", encoding="utf-8") as fe, \
         open(out_unknown, "w", encoding="utf-8") as fx:

        for line in f:
            raw = line.strip()
            if not raw:
                continue

            try:
                obj = json.loads(raw)
            except Exception:
                continue

            rec = read_record(obj)

            u = norm(rec.get("username"))
            e = norm(rec.get("email"))
            p = norm(rec.get("phone"))

            dup_src = obj.get("duplicateSource")
            if dup_src:
                is_user_dup = bool(dup_src.get("username", {}).get("db") or dup_src.get("username", {}).get("file"))
                is_email_dup = bool(dup_src.get("email", {}).get("db") or dup_src.get("email", {}).get("file"))
                is_phone_dup = bool(dup_src.get("phone", {}).get("db") or dup_src.get("phone", {}).get("file"))
            else:
                # Otherwise we detect duplicates INSIDE this duplicates file by frequency
                # (duplicates vs DB only will end up in unknown)
                is_user_dup = username_counts[u] > 1 if u else False
                is_email_dup = email_counts[e] > 1 if e else False
                is_phone_dup = phone_counts[p] > 1 if p else False

            payload = {
                "duplicateFields": {
                    "username": is_user_dup,
                    "email": is_email_dup,
                    "phone": is_phone_dup
                },
                "original": obj
            }

            wrote_any = False

            if is_user_dup:
                fu.write(json.dumps(payload, ensure_ascii=False) + "\n")
                written_user += 1
                wrote_any = True

            if is_email_dup:
                fe.write(json.dumps(payload, ensure_ascii=False) + "\n")
                written_email += 1
                wrote_any = True

            if not wrote_any:
                fx.write(json.dumps(payload, ensure_ascii=False) + "\n")
                written_unknown += 1

    print("✅ Done splitting duplicates:")
    print(f" - Username duplicates: {written_user} -> {out_user}")
    print(f" - Email duplicates:    {written_email} -> {out_email}")
    print(f" - Unknown:             {written_unknown} -> {out_unknown}")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python split_duplicates.py <users.duplicates.jsonl>")
        sys.exit(1)

    main(sys.argv[1])
