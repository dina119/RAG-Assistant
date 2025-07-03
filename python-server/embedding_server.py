from flask import Flask, request, jsonify
import google.generativeai as genai

app = Flask(__name__)

# مفتاح API المجاني من Gemini (Makersuite)
genai.configure(api_key="AIzaSyAoNVA1eVEU3k-xhNtHLhiaNAFwdQZU7AE")

model = genai.GenerativeModel("embedding-001")

@app.route('/get-embedding', methods=['POST'])
def get_embedding():
    data = request.get_json()
    text = data.get('text')

    if not text:
        return jsonify({"error": "No text provided"}), 400

    try:
        response = model.embed_content(content=text)
        return jsonify({"embedding": response["embedding"]})
    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(port=5000)
