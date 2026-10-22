
import json
import sys
from pathlib import Path

# Assumes main.py and the data folder are siblings.
DATA_DIR = Path(__file__).resolve().parent / "PythonBackend"
QUESTS_FILE = DATA_DIR / "quests.json"
TEST_QUESTS_FILE = DATA_DIR / "test_quests.json"


class Database:
    # ==============================================================
    def load(self, task=None, flag="none", sub_cat=False):
        with QUESTS_FILE.open("r", encoding="utf-8") as f:
            data = json.load(f)

        match flag:
            case "none":
                return {"success": False, "error": "No flag assigned"}

            case "test":
                return {"test": data["test"]}

            case "all":
                real_data = data.copy()
                real_data.pop("test", None)
                return real_data

            case _:
                if not task:
                    raise ValueError("No task assigned")

                if task not in data:
                    raise ValueError(f"Task not found: {task}")

                if flag not in data[task]:
                    raise ValueError(f"Flag not found: {flag}")

                if sub_cat:
                    return data[task][flag][sub_cat]

                return data[task][flag]

    # ==============================================================
    def save(self, task=None, flag=None, sub_cat=False,
             data_to_save=None):
        if data_to_save is None:
            raise ValueError("No data to save")

        with QUESTS_FILE.open("r", encoding="utf-8") as f:
            data = json.load(f)

        match flag:
            case None:
                raise ValueError("No flag assigned")

            case "test":
                with TEST_QUESTS_FILE.open("w", encoding="utf-8") as f:
                    json.dump(data_to_save, f, indent=2,
                              ensure_ascii=False)
                return {"message": "Test data saved"}

            case "all":
                data = data_to_save

            case _:
                if not task:
                    raise ValueError("No task assigned")

                if task not in data:
                    raise ValueError(f"Task not found: {task}")

                if flag not in data[task]:
                    raise ValueError(f"Flag not found: {flag}")

                if not sub_cat:
                    raise ValueError("No subcategory assigned")

                data[task][flag][sub_cat] = data_to_save

        DATA_DIR.mkdir(parents=True, exist_ok=True)

        with QUESTS_FILE.open("w", encoding="utf-8") as f:
            json.dump(data, f, indent=2, ensure_ascii=False)

        return {"message": "Saved successfully"}

    # ==============================================================
    def update(self, task=None, flag=None, sub_cat=False,
               data_to_save=None):
        if data_to_save is None:
            raise ValueError("No data to save")

        with QUESTS_FILE.open("r", encoding="utf-8") as f:
            data = json.load(f)

        match flag:
            case None:
                raise ValueError("No flag assigned")

            case "test":
                with TEST_QUESTS_FILE.open("w", encoding="utf-8") as f:
                    json.dump(data["test"], f, indent=2,
                              ensure_ascii=False)
                return {"message": "Test data saved"}

            case "all":
                data = data_to_save

            case _:
                if not task:
                    raise ValueError("No task assigned")

                if task not in data:
                    raise ValueError(f"Task not found: {task}")

                if flag not in data[task]:
                    raise ValueError(f"Flag not found: {flag}")

                if not sub_cat:
                    raise ValueError("No subcategory assigned")

                if flag == "info":
                    raise ValueError(
                        "The info flag cannot be updated; use save instead"
                    )

                target = data[task][flag][sub_cat]

                if not isinstance(target, list):
                    raise ValueError(
                        "The selected subcategory is not a list"
                    )

                target.append(data_to_save)

        with QUESTS_FILE.open("w", encoding="utf-8") as f:
            json.dump(data, f, indent=2, ensure_ascii=False)

        return {"message": "Updated successfully"}


# ==============================================================
# JSON communication layer for the WPF application

db = Database()


def handle_request(request):
    action = request.get("action")

    if action == "load":
        result = db.load(
            task=request.get("task"),
            flag=request.get("flag", "none"),
            sub_cat=request.get("sub_cat", False)
        )
        return {"success": True, "data": result}

    if action == "save":
        result = db.save(
            task=request.get("task"),
            flag=request.get("flag"),
            sub_cat=request.get("sub_cat", False),
            data_to_save=request.get("data")
        )
        return {"success": True, "data": result}

    if action == "update":
        result = db.update(
            task=request.get("task"),
            flag=request.get("flag"),
            sub_cat=request.get("sub_cat", False),
            data_to_save=request.get("data")
        )
        return {"success": True, "data": result}

    if action == "ping":
        return {"success": True, "data": {"message": "Python connected"}}

    raise ValueError(f"Unknown action: {action}")


def main():
    # One JSON request per line, one JSON response per line.
    for line in sys.stdin:
        try:
            request = json.loads(line)
            result = handle_request(request)
            response = result

        except Exception as exc:
            response = {
                "success": False,
                "error": str(exc)
            }

        # stdout is reserved exclusively for JSON responses.
        print(json.dumps(response, ensure_ascii=False), flush=True)


if __name__ == "__main__":
    main()
