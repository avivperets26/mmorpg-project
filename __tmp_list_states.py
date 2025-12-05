import re, pathlib, pprint
path=pathlib.Path(r'Assets/Animations/Knight/AC_Player_Combat_v2.controller')
text=path.read_text(encoding='utf-8')
entries={}
current=None
for line in text.splitlines():
    m=re.match(r"--- !u!(\d+) &(-?\d+)", line)
    if m:
        current={'type':int(m.group(1)), 'id':int(m.group(2)), 'lines':[]}
        entries[current['id']]=current
        continue
    if current is not None:
        current['lines'].append(line)

def get_name(entry):
    for line in entry['lines']:
        m=re.match(r"  m_Name: (.*)", line)
        if m:
            return m.group(1)
    return ''

a_states={eid:e for eid,e in entries.items() if e['type']==1102}
a_sms={eid:e for eid,e in entries.items() if e['type']==1107}
state_name={eid:get_name(e) for eid,e in a_states.items()}
sm_name={eid:get_name(e) for eid,e in a_sms.items()}
sm_states={sm_name[sid]:[] for sid in a_sms}
for sm_id,e in a_sms.items():
    for line in e['lines']:
        m=re.match(r"    m_State: \{fileID: (-?\d+)\}", line)
        if m:
            sid=int(m.group(1))
            sm_states[sm_name[sm_id]].append(state_name.get(sid,sid))
print(sm_states)
