# Testing Customer & User Creation - Quick Start

## ?? **What You Need to Test**

After fixing the Blazor buttons issue, verify that:
1. **Add Customer** button works
2. **Customer form** submits correctly
3. **Data persists** in the database

---

## ?? **Quick Test (5 Minutes)**

### **On Your Server:**

```sh
# 1. Make test script executable
chmod +x test-customer-creation.sh

# 2. Run automated tests
./test-customer-creation.sh http://your-server-ip:7159

# 3. Review results
# ? = Passed
# ? = Failed
# ? = Warning/Check needed
```

### **In Your Browser:**

1. **Open** `http://your-server-ip:7159`
2. **Sign in** with your admin credentials
3. **Navigate** to Customers
4. **Click** "Add Customer" button
5. **Fill in the form:**
   - First Name: Test
   - Last Name: User
   - Phone: 555-123-4567
6. **Click** "Save"
7. **Verify** customer appears in the list

---

## ? **Success Criteria**

### **Button Works:**
- Clicking "Add Customer" opens the form
- No delay or unresponsiveness

### **Form Works:**
- All fields are editable
- Validation shows for required fields
- Save button submits the form

### **Data Persists:**
- After saving, customer appears in list
- Can click customer to view details
- Can edit customer

---

## ?? **If Something Fails**

### **Buttons Still Don't Work:**

```sh
# Clear browser cache
Ctrl + F5 (or Cmd + Shift + R on Mac)

# Check if latest code deployed
git log --oneline -1
# Should show: "Fix critical Blazor buttons issue..."

# Rebuild if needed
docker-compose build webui
docker-compose restart webui
```

### **Form Doesn't Submit:**

**Check Browser Console (F12):**
- Look for red errors
- Common issue: SignalR connection failed

**Check Network Tab:**
- Filter by "WS" (WebSocket)
- Should see `_blazor?id=...` with status 101

### **Data Doesn't Save:**

**Check database connection:**
```sh
docker exec -it tradeflow-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourPassword" -C -Q "SELECT COUNT(*) FROM TradeFlowFSM.dbo.Customers"
```

**Check application logs:**
```sh
docker logs tradeflow-webui --tail 50
```

---

## ?? **Complete Test Checklist**

Open the full guide: [`CUSTOMER_USER_CREATION_TEST_GUIDE.md`](./CUSTOMER_USER_CREATION_TEST_GUIDE.md)

**Quick Checklist:**
- [ ] Navigate to /customers
- [ ] Click "Add Customer"
- [ ] Form loads
- [ ] Fill test data
- [ ] Click "Save"
- [ ] Customer in list
- [ ] Can view details
- [ ] Can edit customer
- [ ] Dark mode works

---

## ?? **What to Test Next**

Once customer creation works:

1. **Other "Add" buttons:**
   - Add Job
   - Add Invoice
   - Add Estimate

2. **Other interactive features:**
   - Modals
   - Dropdowns
   - Calendar
   - Export buttons

3. **Dark mode:**
   - Toggle works
   - Persists on reload

---

## ?? **Expected Performance**

| Action | Expected Time |
|--------|--------------|
| Button click ? Page load | <1 second |
| Form submission | <2 seconds |
| Customer appears in list | Instant |
| Edit customer loads | <1 second |

---

## ?? **Still Having Issues?**

1. **Check the detailed guide:**
   - `CUSTOMER_USER_CREATION_TEST_GUIDE.md`

2. **Run the test script:**
   ```sh
   ./test-customer-creation.sh
   ```

3. **Check the fix documentation:**
   - `BLAZOR_BUTTONS_FIX_RESOLVED.md`

4. **Verify deployment:**
   ```sh
   cd ~/OneManVan
   git status
   docker-compose ps
   ```

---

## ?? **Pro Tips**

### **Browser DevTools**

Open DevTools (F12) before testing:
- **Console tab:** Watch for errors
- **Network tab:** Check API calls
- **Application tab:** Check localStorage for theme

### **Quick Blazor Check**

In browser console, type:
```javascript
Blazor
```

**Should return:** An object with Blazor properties
**If undefined:** Blazor JavaScript didn't load

### **Quick SignalR Check**

In Network tab:
- Filter by "WS" (WebSocket)
- Look for `_blazor?id=...`
- Status should be "101 Switching Protocols"

---

## ?? **Report Template**

After testing, document your results:

```
Date: [DATE]
Server: [IP:PORT]
Browser: [Chrome/Firefox/Edge]

? Working:
- [List what works]

? Issues:
- [List any problems]

?? Notes:
- [Any observations]
```

---

**Ready to test? Start with the Quick Test above!** ??

For detailed step-by-step instructions, see: [`CUSTOMER_USER_CREATION_TEST_GUIDE.md`](./CUSTOMER_USER_CREATION_TEST_GUIDE.md)
