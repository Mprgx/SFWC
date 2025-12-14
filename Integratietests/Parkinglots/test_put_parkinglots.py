import requests


def test_update_parking_lot_unauthorized(login_as_user):
    base = login_as_user["url"]
    url = f"{base}api/parking-lots/1"

    r = requests.put(url, json={}, verify=False)
    assert r.status_code == 401


def test_update_parking_lot_forbidden(auth_headers):
    # normal user (not admin)
    base = "https://localhost:7197/"
    url = f"{base}api/parking-lots/1"

    r = requests.put(url, json={}, headers=auth_headers, verify=False)
    assert r.status_code == 403


def test_update_parking_lot_not_found(auth_headers_admin):
    base = "https://localhost:7197/"
    url = f"{base}api/parking-lots/99999"

    payload = {"name": "Updated"}
    r = requests.put(url, headers=auth_headers_admin,
                     json=payload, verify=False)
    assert r.status_code == 404


def test_update_parking_lot_success(auth_headers_admin):
    base = "https://localhost:7197/"
    create_url = f"{base}api/parking-lots"

    payload = {
        "name": "UpdLot",
        "location": "L",
        "address": "A",
        "capacity": 10,
        "tariff": 1,
        "dayTariff": 5,
        "coordinates": {"latitude": 0, "longitude": 0},
    }

    # create lot
    res = requests.post(create_url, headers=auth_headers_admin,
                        json=payload, verify=False)
    res.raise_for_status()
    lot_id = res.json()["id"]

    # update
    update_url = f"{base}api/parking-lots/{lot_id}"
    r = requests.put(update_url, headers=auth_headers_admin,
                     json={"name": "UpdatedName"}, verify=False)

    assert r.status_code == 200


def test_update_parking_lot_wrong_token():
    base = "https://localhost:7197/"
    url = f"{base}api/parking-lots/1"

    r = requests.put(url, headers={"Authorization": "Fake"}, json={
                     "name": "X"}, verify=False)
    assert r.status_code == 401
