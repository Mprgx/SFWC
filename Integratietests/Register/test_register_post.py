import uuid
import requests

def _unique_username():
    return uuid.uuid4().hex[:10]

def test_register_creates_user_returns_201_and_json_contract(user_session):
    url = user_session['url'] + 'register'
    username = _unique_username('ok')
    password = 'Smpl3Pw!'
    payload = {
        'username': username,
        'password': password,
        'name': 'Test Name'
    }
    r = requests.post(url, json=payload)
    assert r.status_code == 201
    assert r.headers.get('Content-Type', '').startswith('application/json')
    assert 'status' in r.json() or 'message' in r.json()

    login = requests.post(user_session['url'] + 'login', json={'username': username, 'password': password})
    assert login.status_code == 200
    assert 'session_token' in login.json()

def test_register_missing_fields_returns_400(user_session):
    url = user_session['url'] + 'register'
    r = requests.post(url, json={})
    assert r.status_code == 400

def test_register_duplicate_username_returns_409(user_session):
    url = user_session['url'] + 'register'
    username = _unique_username()
    base = {'username': username, 'password': 'abc12345', 'name': 'Duplicate'}

    r1 = requests.post(url, json=base)
    assert r1.status_code == 201
    
    r2 = requests.post(url, json=base)
    assert r2.status_code == 409
