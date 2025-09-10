#!/usr/bin/env python3
# send_sms.py
import json
import logging
import os
import sys
from datetime import datetime, timezone
from typing import Dict, Any, List

import requests

# ======= CONFIG (edit these) =======
BASE_URL = "https://api.textbee.dev/api/v1"
API_KEY = "f8a41718-e4c9-4567-b24c-eb9e33dd106d"        # <-- replace
DEVICE_ID = "68bf24d3c3eec74784421f4c"    # <-- replace

# Optional: override recipients/message by CLI:
#   python send_sms.py +15551234567 "hello"
DEFAULT_RECIPIENTS = ["+16617420018"]
DEFAULT_MESSAGE = "Hello from TextBee!"

# ======= LOGGING =======
os.makedirs("logs", exist_ok=True)
LOG_FILE_TXT = "logs/textbee_sender.log"
LOG_FILE_JSONL = "logs/textbee_sender.jsonl"

logging.basicConfig(
    level=logging.INFO,
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

def send_sms(recipients: List[str], message: str) -> Dict[str, Any]:
    url = f"{BASE_URL}/gateway/devices/{DEVICE_ID}/send-sms"
    headers = {"x-api-key": API_KEY, "Content-Type": "application/json"}
    payload = {"recipients": recipients, "message": message}
    resp = requests.post(url, json=payload, headers=headers, timeout=30)
    try:
        data = resp.json()
    except Exception:
        data = {"raw_text": resp.text}
    event = {
        "ts": now_iso(),
        "event": "sent_sms",
        "http_status": resp.status_code,
        "request": payload,
        "response": data,
    }
    append_jsonl(event)
    logging.info("Sent to %s | HTTP %s", recipients, resp.status_code)
    return data

def main():
    if API_KEY.startswith("YOUR_") or DEVICE_ID.startswith("YOUR_"):
        logging.error("Please set API_KEY and DEVICE_ID in send_sms.py")
        sys.exit(1)

    recipients = DEFAULT_RECIPIENTS
    message = DEFAULT_MESSAGE
    if len(sys.argv) >= 2:
        recipients = [sys.argv[1]]
    if len(sys.argv) >= 3:
        message = sys.argv[2]

    data = send_sms(recipients, message)
    print(json.dumps(data, indent=2, ensure_ascii=False))

if __name__ == "__main__":
    main()
