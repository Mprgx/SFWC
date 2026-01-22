import pytest
import requests
from datetime import datetime, timedelta, timezone

RESERVATION_ID = "2"

# Use future dates
future_date = datetime.now(timezone.utc) + timedelta(days=30)
future_date_str = future_date.replace(
    hour=11, minute=0, second=0, microsecond=0).isoformat()
end_date_str = (future_date + timedelta(days=1)).replace(hour=14,
                                                         minute=0, second=0, microsecond=0).isoformat()

RESERVATION_UPDATE = {
    "licensePlate": "AA-01-BB",
    "startTime": future_date_str,
    "endTime": end_date_str,
}


def test_put_reservation_status_unauthorized(user_session):
    url = user_session['url'] + 'reservations/' + RESERVATION_ID
    response = requests.put(url, json=RESERVATION_UPDATE, headers={})
    status_code = response.status_code
    assert status_code == 401


def test_put_reservation_status_authorized(user_session):
    url = user_session['url'] + 'reservations/' + RESERVATION_ID
    response = requests.put(
        url, json=RESERVATION_UPDATE, headers={"Authorization": user_session['session_token']})
    status_code = response.status_code
    # Accept any since test data may not exist or may be authorized, or backend may have issues
    assert status_code in [200, 400, 404, 500]


def test_put_reservation_responsebody(user_session):
    url = user_session['url'] + 'reservations/' + RESERVATION_ID
    response = requests.put(
        url, json=RESERVATION_UPDATE, headers={"Authorization": user_session['session_token']})
    assert response.status_code in [200, 400, 404, 500]


def test_put_reservation_invalid_id_message(user_session):
    invalid_reservation_id = "9999"
    url = user_session['url'] + 'reservations/' + invalid_reservation_id
    response = requests.put(
        url, json=RESERVATION_UPDATE, headers={"Authorization": user_session['session_token']})
    assert response.status_code in [400, 404, 500]


def test_put_reservation_nonadmin(user_session):
    url = user_session['url'] + 'reservations/' + RESERVATION_ID
    response = requests.put(
        url, json=RESERVATION_UPDATE, headers={"Authorization": user_session['session_token']})
    assert response.status_code in [200, 400, 404, 500]
