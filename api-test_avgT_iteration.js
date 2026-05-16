import http from 'k6/http';
import { check } from 'k6';
import { Trend } from 'k6/metrics'; 

const iterDuration = new Trend('custom_iter_duration'); 

export const options = {
 thresholds: {
        http_req_duration: ['p(99)< 5000'], // 99% of requests should be below 5000ms
        http_req_failed: ['rate<0.05'], // http errors should be less than 5%
    },

    scenarios: {
        api_test: {
            executor: 'ramping-vus',
            stages: [
                { duration: '30s', target: 10   },  // warm-up
            ],        
        }
}
}

export function setup() {
    // Erst Token holen
    const body = JSON.stringify({ username: 'admin', password: 'password123', 
                                  email: 'admin@user.com', customClaims: { admin: "true" } });
    const res = http.post('http://localhost:80/api/auth/token', body,
        { headers: { 'Content-Type': 'application/json' } });
    console.log('setup auth response:', res.status, res.body);
    const token = JSON.parse(res.body).data.token;

    // 200 Mitarbeiter anlegen
    for (let i = 0; i < 200; i++) {
        const emp = JSON.stringify({ firstName: `Seed${i}`, lastName: 'Test',
            email: `seed${i}@test.com`, isActive: true, birthDate: '1990-01-01' });
        http.post('http://localhost:80/api/employees', emp,
            { headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' } });
    }
    return { token }; // Rückgabe → steht in default(data) zur Verfügung
}

export default function () {
    const start = Date.now(); // Startzeit der Iteration

const p     = { timeout: '10s' };
    const pJson = { timeout: '10s', headers: { 'Content-Type': 'application/json' } };

    const res0 = http.get('http://localhost:80/api/employees', p);
    check(res0, {
        'res0 GET /employees status 200': (r) => r.status === 200,
    });

    const body = JSON.stringify({ username: `testuser${__VU}`, password: 'password123', email: `testuser${__ITER}@user.com`, customClaims: { admin: "true", department: 'IT' } });

    const res1 = http.post('http://localhost:80/api/auth/token', body, pJson);
    check(res1, {
        'res1 POST /auth/token status 200': (r) => r.status === 200,
        'response body contains token': (r) => r.body != null && r.body.includes('token'),
    });

    if (res1.status !== 200 || res1.body == null) {
        console.log(`auth failed: status=${res1.status} body=${res1.body}`);
        return;
    }
    const token = JSON.parse(res1.body).data.token;
    const pAuth     = { timeout: '10s', headers: { 'Authorization': `Bearer ${token}` } };
    const pAuthJson = { timeout: '10s', headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' } };

    const res2 = http.get('http://localhost:80/api/employees/search?search=isActive', pAuth);
    check(res2, {
        'res2 GET /search status 200': (r) => r.status === 200,
    });

    const res3 = http.get('http://localhost:80/api/employees/birthDate?birthDate=2011-01-01', pAuth);
    check(res3, {
        'res3 GET /birthDate status 200': (r) => r.status === 200,
    });

    const newEmployee = JSON.stringify({ firstName: `John${__VU}${__ITER}`, lastName: 'Doe', email: `john.doe${__ITER}@example.com`, isActive: true, birthDate: randomDate(1965, 2010) });
    const res4 = http.post('http://localhost:80/api/employees', newEmployee, pAuthJson);
    const id = res4.status === 201 ? JSON.parse(res4.body).data.id : null;
    check(res4, {
        'res4 POST /employees status 201': (r) => r.status === 201,
        'res4 POST /employees response body is correct': (r) => {
            try { return JSON.parse(r.body).message.includes("New employee created"); }
            catch { return false; }
        },
    });

    const patchEmployee = JSON.stringify({ firstName: `JohnRR${__VU}`, lastName: 'Doe', email: `john.doeRR${__ITER}@example.com`, isActive: true, birthDate: randomDate(1980, 2010) });
    const res5 = id ? http.patch(`http://localhost:80/api/employees/${id}`, patchEmployee, pAuthJson) : null;
    res5 && check(res5, {
        'res5 PATCH /employees/:id status 200': (r) => r.status === 200,
    });

    const res6 = id ? http.del(`http://localhost:80/api/employees/${id}`, null, pAuth) : null;
    res6 && check(res6, {
        'res6 DELETE /employees/:id status 204': (r) => r.status === 204,
    });

    // sleep(1); // Pause zwischen Iterationen → verhindert Pool-Exhaustion

    iterDuration.add(Date.now() - start); // Dauer ohne sleep → T_gemessen
}

function randomDate(startYear, endYear) {
    const year  = Math.floor(Math.random() * (endYear - startYear + 1)) + startYear;
    const month = Math.floor(Math.random() * 12) + 1;
    const day   = Math.floor(Math.random() * 28) + 1; // 28 = sicher für alle Monate
    
    const mm = String(month).padStart(2, '0');
    const dd = String(day).padStart(2, '0');
    
    return `${year}-${mm}-${dd}`;
}; 






