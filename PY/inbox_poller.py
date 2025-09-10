#!/usr/bin/env python3
# inbox_poller.py
import json
import logging
import os
import signal
import sys
import time
from datetime import datetime, timezone
from typing import Dict, Any, List, Optional, Set

import requests

# ======= CONFIG (edit these) =======
BASE_URL = "https://api.textbee.dev/api/v1"
API_KEY = "f8a41718-e4c9-4567-b24c-eb9e33dd106d"        # <-- replace
DEVICE_ID = "68bf24d3c3eec74784421f4c"    # <-- replace
POLL_SECONDS = 10               # how often to poll

# Endpoints:
#  - /messages   : usually includes sent + received
#  - /get-received-sms : received only (if your account supports it)
MESSAGES_URL = f"{BASE_URL}/gateway/devices/{DEVICE_ID}/messages"
RECEIVED_URL = f"{BASE_URL}/gateway/devices/{DEVICE_ID}/get-received-sms"
USE_RECEIVED_ONLY = False       # start with False to confirm data returns

# ======= LOGGING & STATE =======
os.makedirs("logs", exist_ok=True)
os.makedirs("state", exist_ok=True)

LOG_FILE_TXT = "logs/textbee_inbox.log"
LOG_FILE_JSONL = "logs/textbee_inbox.jsonl"
SEEN_FILE = "state/seen_ids.json"   # prevents duplicate logging

logging.basicConfig(
    level=logging.DEBUG,  # verbose so you can see what's happening
    format="%(asctime)s %(levelname)s %(message)s",
    handlers=[
        logging.StreamHandler(sys.stdout),
        logging.FileHandler(LOG_FILE_TXT, encoding="utf-8"),
    ],
)

def now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()

def append_jsonl(record: Dict[str, Any]) -> None:
    with open(LOG_FILE_JSONL, "a", encoding="utf-8") as f:
        f.write(json.dumps(record, ensure_ascii=False) + "\n")

def load_seen() -> Set[str]:
    try:
        with open(SEEN_FILE, "r", encoding="utf-8") as f:
            return set(json.load(f).get("ids", []))
    except Exception:
        return set()

def save_seen(ids: Set[str]) -> None:
    with open(SEEN_FILE, "w", encoding="utf-8") as f:
        json.dump({"ids": sorted(ids)}, f)

def guess_id(m: Dict[str, Any]) -> Optional[str]:
    for k in ("id", "message_id", "_id", "uuid"):
        v = m.get(k)
        if isinstance(v, str) and v.strip():
            return v
    return None

def normalize_messages(data: Any) -> List[Dict[str, Any]]:
    if isinstance(data, list):
        return [x for x in data if isinstance(x, dict)]
    if isinstance(data, dict):
        for key in ("messages", "data", "results", "items"):
            v = data.get(key)
            if isinstance(v, list):
                return [x for x in v if isinstance(x, dict)]
        # Single-message fallback shape
        if any(k in data for k in ("from", "to", "message", "text", "body", "timestamp", "created_at")):
            return [data]
    return []

def human_summary(msg: Dict[str, Any]) -> str:
    frm = msg.get("from") or msg.get("sender") or "?"
    to = msg.get("to") or msg.get("recipient") or "?"
    body = msg.get("message") or msg.get("text") or msg.get("body") or ""
    ts = msg.get("timestamp") or msg.get("created_at") or now_iso()
    body_short = body[:120].replace("\n", " ")
    return f"{ts} | from={frm} -> to={to} | {body_short}"

class InboxPoller:
    def __init__(self):
        if API_KEY.startswith("YOUR_") or DEVICE_ID.startswith("YOUR_"):
            logging.error("Please set API_KEY and DEVICE_ID in inbox_poller.py")
            sys.exit(1)

        self.session = requests.Session()
        self.session.headers.update({"x-api-key": API_KEY, "Content-Type": "application/json"})
        self.seen: Set[str] = load_seen()
        self._stop = False

    def _get(self, url: str, label: str) -> Dict[str, Any]:
        logging.debug("GET %s", url)
        try:
            r = self.session.get(url, timeout=30)
            try:
                data = r.json()
            except Exception:
                data = {"raw_text": r.text}
            logging.info("%s -> HTTP %s", label, r.status_code)
            append_jsonl({"ts": now_iso(), "event": f"{label}_poll", "status": r.status_code, "payload": data})
            return {"status": r.status_code, "data": data}
        except Exception as e:
            logging.error("%s failed: %s", label, e)
            append_jsonl({"ts": now_iso(), "event": f"{label}_poll_error", "error": str(e)})
            return {"status": None, "data": None}

    def poll_once(self):
        url = RECEIVED_URL if USE_RECEIVED_ONLY else MESSAGES_URL
        result = self._get(url, "poll")
        data = result["data"]
        msgs = normalize_messages(data)
        logging.info("Normalized messages found: %d", len(msgs))
        new_logged = 0

        for m in msgs:
            mid = guess_id(m)
            if mid and mid in self.seen:
                continue
            direction = (m.get("direction") or m.get("type") or "received").lower()
            append_jsonl({"ts": now_iso(), "event": f"{direction}_sms", "message": m, "source": "poll"})
            logging.info("[LOGGED] %s", human_summary(m))
            if mid:
                self.seen.add(mid); new_logged += 1

        if new_logged:
            save_seen(self.seen)
            logging.info("Saved %d new IDs (total seen: %d)", new_logged, len(self.seen))

    def run_forever(self):
        logging.info("📥 Inbox poller running. Logs: %s | %s", LOG_FILE_TXT, LOG_FILE_JSONL)
        logging.info("Polling every %ss. Ctrl+C to stop.", POLL_SECONDS)

        def _stop(_sig, _frm):
            logging.info("Stopping poller...")
            self._stop = True
            sys.exit(0)

        signal.signal(signal.SIGINT, _stop)
        signal.signal(signal.SIGTERM, _stop)

        while not self._stop:
            self.poll_once()
            time.sleep(POLL_SECONDS)

def main():
    poller = InboxPoller()
    poller.run_forever()

if __name__ == "__main__":
    main()
