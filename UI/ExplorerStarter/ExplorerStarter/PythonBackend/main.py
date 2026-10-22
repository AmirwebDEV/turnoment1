import json

class Task_Database:
#======================================================================
    def load(self,task=None, flag=None, sub_cat=False):
        
        with open("data/quests.json", "r") as f:
            data = json.load(f)
        
        match flag:
            case None:
                print("no flags assaigned")
            
            case "test":
                return {"test":data["test"]}
            
            case "all":
                real_data= data.copy()
                real_data.pop("test")
                return real_data
            
            case _:
                if not task:
                    print("no task assaigned")
                else:
                    if flag not in data[task].keys():
                        print("flag not found")
                    else:
                        if sub_cat:
                            return data[task][flag][sub_cat]
                        else:
                            return data[task][flag]
#======================================================================
    def save(self,task=None, flag=None, sub_cat=False, data_to_save=None):
        
        if data_to_save is None:
            print("no data to save")
            return
        else:
            with open("data/quests.json", "r") as f:
                data = json.load(f)
            
            match flag:
                case None:
                    print("no flags assaigned")
                
                case "test":
                    with open("data/test_quests.json", "w") as f:
                        json.dump(data, f)
                        print("check test_quests.json for test data")
                    return
                
                case "all":
                    data = data_to_save
                    print("saving eveerything")
                    
                case _:
                    if not task:
                        print("no task assaigned")
                    else:
                        if flag not in data[task].keys():
                            print("flag not found")
                        else:
                            if not sub_cat:
                                print("no sub_cat assaigned")
                            else:
                                data[task][flag][sub_cat] = data_to_save
                                print(f"saving data to {task} -> {flag} -> {sub_cat}")   
                            
            with open("data/quests.json", "w") as f:
                json.dump(data, f)
                print("saved successfully")
#======================================================================
    def update(self,task=None, flag=None, sub_cat=False, data_to_save=None):
        
        if data_to_save is None:
            print("no data to save")
            return
        else:
            with open("data/quests.json", "r") as f:
                data = json.load(f)
            
            match flag:
                case None:
                    print("no flags assaigned")
                
                case "test":
                    with open("data/test_quests.json", "w") as f:
                        json.dump(data["test"], f)
                        print("check test_quests.json for test data")
                    return
                
                case "all":
                    data = data_to_save
                    print("saving eveerything")
                    
                case _:
                    if not task:
                        print("no task assaigned")
                    else:
                        if flag not in data[task].keys():
                            print("flag not found")
                        else:
                            if not sub_cat:
                                print("no sub_cat assaigned")
                            elif flag == "info":
                                print("info flag cannot be updated (must be overridden, use save function instead)")
                            else:
                                data[task][flag][sub_cat].append(data_to_save)
                                print(f"adding data to {task} -> {flag} -> {sub_cat}")   
                            
            with open("data/quests.json", "w") as f:
                json.dump(data, f)
                print("updated successfully")
                
    def remove(self,task=None, flag=None, sub_cat=False):
        
        with open("data/quests.json", "r") as f:
            data = json.load(f)
        
        match flag:
            case None:
                print("no flags assaigned")
            
            case "test":
                print("you may need your test subject so, NO")
            
            case "all":
                data = {}
            
            case _:
                if not task:
                    print("no task assaigned")
                else:
                    if flag not in data[task].keys():
                        print("flag not found")
                    else:
                        if sub_cat:
                            data[task][flag][sub_cat]=[]
                        else:
                            for sub_cat in data[task][flag].keys():
                                data[task][flag][sub_cat]=[]
                                
        with open("data/quests.json", "w") as f:
            json.dump(data, f)
                                
        