import requests
import uuid
import re

# Delete parking lot tests


def test_delete_parking_lot_unauthorized(login_as_user):
    url = login_as_user["url"] + "parking-lots/1"
    r = requests.delete(url, verify=False)
    assert r.status_code == 401


def test_delete_parking_lot_forbidden(auth_headers, login_as_user):
    url = login_as_user["url"] + "parking-lots/1"
    r = requests.delete(url, headers=auth_headers, verify=False)
    assert r.status_code == 403


def test_delete_parking_lot_not_found(login_as_admin):
    headers = {"Authorization": login_as_admin["session_token"]}
    url = login_as_admin["url"] + "parking-lots/99999"
    r = requests.delete(url, headers=headers, verify=False)
    assert r.status_code == 404


def test_delete_parking_lot_success(login_as_admin):
    headers = {"Authorization": login_as_admin["session_token"]}

    create_url = login_as_admin["url"] + "parking-lots"
    payload = {
        "name": "DeleteLot",
        "location": "Loc",
        "address": "Addr",
        "capacity": 5,
        "tariff": 1,
        "dayTariff": 5,
        "coordinates": {"latitude": 1, "longitude": 1},
    }

    created = requests.post(create_url, headers=headers,
                            json=payload, verify=False)

    assert created.status_code == 201

    lot_id = created.json()["id"]

    # delete
    url = login_as_admin["url"] + f"parking-lots/{lot_id}"
    r = requests.delete(url, headers=headers, verify=False)
    assert r.status_code == 200



def test_delete_parking_lot_wrong_token(login_as_user):
    url = login_as_user["url"] + "parking-lots/1"
    r = requests.delete(
        url, headers={"Authorization": "Bearer Fake"}, verify=False)
    assert r.status_code == 401


# Delete parking session tests
def test_delete_session_unauthorized(login_as_user):
    url = login_as_user["url"] + "parking-lots/1/sessions/" + str(uuid.uuid4())
    r = requests.delete(url, verify=False)
    assert r.status_code == 401


def test_delete_session_forbidden(auth_headers, login_as_user):
    url = login_as_user["url"] + "parking-lots/1/sessions/" + str(uuid.uuid4())
    r = requests.delete(url, headers=auth_headers, verify=False)
    assert r.status_code == 403


def test_delete_session_not_found(login_as_admin):
    headers = {"Authorization": login_as_admin["session_token"]}
    url = login_as_admin["url"] + \
        "parking-lots/1/sessions/" + str(uuid.uuid4())
    r = requests.delete(url, headers=headers, verify=False)
    assert r.status_code == 404


def test_delete_session_wrong_token(login_as_user):
    url = login_as_user["url"] + "parking-lots/1/sessions/" + str(uuid.uuid4())
    r = requests.delete(
        url, headers={"Authorization": "Bearer Fake"}, verify=False)
    assert r.status_code == 401


def test_delete_session_success_placeholder(login_as_admin):
    # only succeeds if you have real test data
    headers = {"Authorization": login_as_admin["session_token"]}
    url = login_as_admin["url"] + \
        "parking-lots/1/sessions/" + str(uuid.uuid4())
    r = requests.delete(url, headers=headers, verify=False)
    assert r.status_code in (200, 404)
