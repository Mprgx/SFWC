import requests
import uuid

# get all parking lots tests


def test_get_all_parking_lots_unauthorized(login_as_user):
    url = login_as_user["url"] + "parking-lots"
    r = requests.get(url, verify=False)
    assert r.status_code == 401


def test_get_all_parking_lots_authorized(auth_headers, login_as_user):
    url = login_as_user["url"] + "parking-lots"
    r = requests.get(url, headers=auth_headers, verify=False)
    assert r.status_code == 200


def test_get_all_parking_lots_returns_list(auth_headers, login_as_user):
    url = login_as_user["url"] + "parking-lots"
    r = requests.get(url, headers=auth_headers, verify=False)
    assert isinstance(r.json(), list)


def test_get_all_parking_lots_response_is_json(auth_headers, login_as_user):
    url = login_as_user["url"] + "parking-lots"
    r = requests.get(url, headers=auth_headers, verify=False)
    assert r.headers.get("Content-Type", "").startswith("application/json")


def test_get_all_parking_lots_missing_header(login_as_user):
    url = login_as_user["url"] + "parking-lots"
    r = requests.get(url, verify=False)
    assert r.status_code == 401

# get by id tests


def test_get_parking_lot_by_id_unauthorized(login_as_user):
    url = login_as_user["url"] + "parking-lots/1"
    r = requests.get(url, verify=False)
    assert r.status_code == 401


def test_get_parking_lot_by_id_not_found(auth_headers, login_as_user):
    url = login_as_user["url"] + "parking-lots/999999"
    r = requests.get(url, headers=auth_headers, verify=False)
    assert r.status_code == 404


def test_get_parking_lot_by_id_success(login_as_admin):
    # create lot
    create_url = login_as_admin["url"] + "parking-lots"
    headers_admin = {"Authorization": login_as_admin["session_token"]}

    payload = {
        "name": "GetLot",
        "location": "Center",
        "address": "A Street",
        "capacity": 10,
        "tariff": 1,
        "dayTariff": 5,
        "coordinates": {"latitude": 1, "longitude": 2},
    }

    created = requests.post(
        create_url, headers=headers_admin, json=payload, verify=False)
    lot_id = created.json()["id"]

    url = login_as_admin["url"] + f"parking-lots/{lot_id}"
    r = requests.get(url, headers=headers_admin, verify=False)

    assert r.status_code == 200
    assert r.json()["name"] == "GetLot"


def test_get_parking_lot_by_id_wrong_token(login_as_user):
    url = login_as_user["url"] + "parking-lots/1"
    r = requests.get(
        url, headers={"Authorization": "Bearer FAKE"}, verify=False)
    assert r.status_code == 401


def test_get_parking_lot_by_id_json(auth_headers, login_as_user):
    url = login_as_user["url"] + "parking-lots/1"
    r = requests.get(url, headers=auth_headers, verify=False)
    if r.status_code == 200:
        assert r.headers.get("Content-Type", "").startswith("application/json")

# get sessions tests


def test_get_sessions_unauthorized(login_as_user):
    url = login_as_user["url"] + "parking-lots/1/sessions"
    r = requests.get(url, verify=False)
    assert r.status_code == 401


def test_get_sessions_forbidden_user(auth_headers, login_as_user):
    url = login_as_user["url"] + "parking-lots/1/sessions"
    r = requests.get(url, headers=auth_headers, verify=False)
    assert r.status_code == 403


def test_get_sessions_not_found(login_as_admin):
    headers = {"Authorization": login_as_admin["session_token"]}
    url = login_as_admin["url"] + "parking-lots/99999/sessions"
    r = requests.get(url, headers=headers, verify=False)
    assert r.status_code == 404


def test_get_sessions_success(login_as_admin):
    headers = {"Authorization": login_as_admin["session_token"]}
    url = login_as_admin["url"] + "parking-lots/1/sessions"
    r = requests.get(url, headers=headers, verify=False)
    assert r.status_code in (200, 404)


def test_get_sessions_response_json(login_as_admin):
    headers = {"Authorization": login_as_admin["session_token"]}
    url = login_as_admin["url"] + "parking-lots/1/sessions"
    r = requests.get(url, headers=headers, verify=False)
    if r.status_code == 200:
        assert r.headers.get("Content-Type", "").startswith("application/json")

# get session by id tests


def test_get_session_by_id_unauthorized(login_as_user):
    url = login_as_user["url"] + "parking-lots/1/sessions/" + str(uuid.uuid4())
    r = requests.get(url, verify=False)
    assert r.status_code == 401


def test_get_session_by_id_forbidden(auth_headers, login_as_user):
    url = login_as_user["url"] + "parking-lots/1/sessions/" + str(uuid.uuid4())
    r = requests.get(url, headers=auth_headers, verify=False)
    assert r.status_code == 403


def test_get_session_by_id_not_found(login_as_admin):
    headers = {"Authorization": login_as_admin["session_token"]}
    url = login_as_admin["url"] + \
        "parking-lots/1/sessions/" + str(uuid.uuid4())
    r = requests.get(url, headers=headers, verify=False)
    assert r.status_code == 404


def test_get_session_by_id_wrong_token(login_as_user):
    url = login_as_user["url"] + "parking-lots/1/sessions/" + str(uuid.uuid4())
    r = requests.get(
        url, headers={"Authorization": "Bearer FAKE"}, verify=False)
    assert r.status_code == 401


def test_get_session_by_id_response_json(login_as_admin):
    headers = {"Authorization": login_as_admin["session_token"]}
    url = login_as_admin["url"] + \
        "parking-lots/1/sessions/" + str(uuid.uuid4())
    r = requests.get(url, headers=headers, verify=False)
    if r.status_code == 200:
        assert r.headers.get("Content-Type", "").startswith("application/json")
