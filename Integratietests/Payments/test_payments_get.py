import uuid
import requests

def test_get_payments_requires_auth(_data):
    url = _data['url'] + 'payments'
    r = requests.get(url)
    assert r.status_code == 401

def test_get_payments_returns_user_payments(_data):
    url = _data['url'] + 'payments'
    auth_headers = {"Authorization": _data["api_key"]}

    transaction_id = uuid.uuid4().hex[:10]
    new_payment = {"transaction": transaction_id, "amount": 75.25}

    seed_response = requests.post(url, json=new_payment, headers=auth_headers)
    assert seed_response.status_code == 201, f"{seed_response.status_code} {seed_response.text}"

    get_response = requests.get(url, headers=auth_headers)
    assert get_response.status_code == 200
    assert get_response.headers.get("Content-Type", "").startswith("application/json")

    payments_list = get_response.json
    assert isinstance(payments_list, list), "GET /payments should return a JSON array"

    matching_payments = [p for p in payments_list if p.get("transaction") == transaction_id]
    assert matching_payments, f"Expected seesed transaction {transaction_id} in GET /payments result"

    payment = matching_payments[0]
    assert "transaction" in payment
    assert "amount" in payment
    assert "initiator" in payment