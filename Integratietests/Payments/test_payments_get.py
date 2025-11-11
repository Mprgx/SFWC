import uuid
import requests

def test_get_payments_requires_auth(user_session):
    url = user_session['url'] + 'payments'
    r = requests.get(url)
    assert r.status_code == 401
    assert r.headers.get('Content-Type', '').startswith('application/json')

def test_get_payments_returns_user_payments(user_session):
    url = user_session['url'] + 'payments'
    headers = {"Authorization":user_session["session_token"]}
    payload = {"transaction": uuid.uuid4().hex[:12], "amount": 75.25}

    seed_response = requests.post(url, json=payload, headers=headers)
    assert seed_response.status_code == 201, f"{seed_response.status_code}: {seed_response.text}"

    get_response = requests.get(url, headers=headers)
    assert get_response.status_code == 200
    assert get_response.headers.get("Content-Type", "").startswith("application/json")

    payments = get_response.json()
    assert isinstance(payments, list), "GET /payments should return a JSON array"

    matches = [p for p in payments if p.get("transaction") == payload["transaction"]]
    assert matches, f"Expected seeded transaction {payload['transaction']} in GET /payments result"

    payment = matches[0]
    assert "transaction" in payment
    assert "amount" in payment
    assert "initiator" in payment

